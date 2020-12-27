import sys
import os
import signal
import platform
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.common.exceptions import TimeoutException
from selenium.webdriver.support import expected_conditions as EC
import subprocess
import time
import argparse
import os.path
from os import path

parser = argparse.ArgumentParser(description='Run Servirtium.NET.')
parser.add_argument("mode", help="Servirtium's mode of operation, i.e. recording a new script or playing an existing one back", choices = ["record", "playback", "direct"])
parser.add_argument("-p", "--port", help="The port Servirtium will run on", type=int, default=1234)
parser.add_argument("-d", "--chromedriver", help="The location of the Selenium Chrome Webdriver executable - omit to use one that's on the system PATH")
parser.add_argument("-t", "--testpage", help="The page to point chrome at to run the tests, use the '%%s' token where the port should be specified. To point back at the original todobackend, specify http://www.todobackend.com/specs/index.html?http://localhost:%%s/todos", default="https://servirtium.github.io/compatibility-suite/#port=%s")
parser.add_argument("--backend", help="The real todo backend implementation, only used in 'record' or 'direct' mode", default="http://todo-backend-sinatra.herokuapp.com")
parser.add_argument("--timeoutseconds", help="Number of seconds to wait before giving up on a successful run and ending the test run", type=int, default=20)
parser.add_argument("--serverversion", help="Version of Standalone.Server to use - this is the version it will pull from Nuget. Setting this will make the script to try to get the Servirtium.StandaloneServer from nuget, rather than building from source.")
parser.add_argument("--nuget", help="Nuget.exe executable path. Default is to use one on the system PATH", default="nuget")

args = parser.parse_args()

dotnet_platform = "netcoreapp3.1"

dotnet_process = None
# os.environ["HTTP_PROXY"] = "http://localhost:%s" %(args.port)
browser_url = args.testpage %(args.port)

if args.mode == "direct":
    browser_url = "http://www.todobackend.com/specs/index.html?%s" %(args.backend)
else:
    servirtium_args = []
    servirtium_launch_command = []

    if args.mode == "record":
        servirtium_args = ["record", args.backend, "http://localhost:%s" %(args.port), "--urls=http://*:%s" %(args.port)]
    elif args.mode == "playback":
        servirtium_args = ["playback", args.backend, "--urls=http://*:%s" %(args.port)]
    if args.serverversion:
        #Install from Nuget
        subprocess.call([args.nuget, "install", "Servirtium.StandaloneServer", "-OutputDirectory", "executable-packages", "-PreRelease", "-NonInteractive", "-Version", args.serverversion])
        serverpath = "executable-packages/Servirtium.StandaloneServer.%s/lib/%s/Servirtium.StandaloneServer.exe" %(args.serverversion, dotnet_platform)
        servirtium_launch_command = [serverpath]
    else:
        #Build the Servirtium server first if required
        subprocess.call(["dotnet", "build", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj"])
        servirtium_launch_command = ["dotnet", "run", "--project", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj", "--no-build", "--"]
    
    # TODO check that .NET process is already started.
    with open('compatibility_suite_servirtium_server.log', "w") as outfile:
        dotnet_process = subprocess.Popen(servirtium_launch_command + servirtium_args, stdout = outfile, stdin = subprocess.PIPE)
    print("Servirtium.NET server %s (from %s) started - process id: %s " %(args.mode, str(dotnet_process.pid), "Nuget - version "+args.serverversion if args.serverversion else "source"))

chrome_options = webdriver.ChromeOptions()
#chrome_options.add_argument("--proxy-server=%s" % "localhost:%s" %(args.port))
chrome_options.add_argument("--auto-open-devtools-for-tabs")


if args.chromedriver:
    chrome = webdriver.Chrome(executable_path=args.chromedriver, options=chrome_options)
else:
    chrome = webdriver.Chrome(options=chrome_options)


chrome.get(browser_url)
try:
    element = WebDriverWait(chrome, args.timeoutseconds).until(
        EC.text_to_be_present_in_element((By.CLASS_NAME, "passes"), "16")
    )
    print("Compatibility suite: all 16 tests passed")

except TimeoutException as ex:
    print("Compatibility suite: did not finish with 16 passes. See open browser frame.")

# TODO warn that .NET process was not started.

print("mode: " + args.mode)


if dotnet_process is not None:
    print("Killing Servirtium.NET")
    dotnet_process.communicate(input="x".encode("utf-8"))

print("Closing Selenium")
chrome.quit()
print("All done.")