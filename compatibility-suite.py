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

dotnet_process = None
# os.environ["HTTP_PROXY"] = "http://localhost:1234"
if len(sys.argv) > 1:
   if sys.argv[1] == "record":
       # TODO check that .NET process is already started.
       url = "http://localhost:1234"
       with open('myfile', "w") as outfile:
           dotnet_process = subprocess.Popen(["dotnet", "run", "--project", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj", "--", "record", "http://todo-backend-sinatra.herokuapp.com", "http://localhost:1234", "--urls=http://*:1234"], stdout = outfile, stdin = subprocess.PIPE)
       print(".NET record process: "+str(dotnet_process.pid))
   elif sys.argv[1] == "playback":
       url = "http://localhost:1234"
       with open('myfile', "w") as outfile:
           dotnet_process = subprocess.Popen(["dotnet", "run", "--project", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj", "--", "playback", "http://todo-backend-sinatra.herokuapp.com", "--urls=http://*:1234"], stdout = outfile, stdin = subprocess.PIPE)
       print(".NET playback process: "+str(dotnet_process.pid))
   elif sys.argv[1] == "direct":
       print("showing reference Sinatra app online without Servirtium in the middle")
       url = "https://todo-backend-sinatra.herokuapp.com"
   else:
       print("Second arg should be record or playback")
       exit(10)
else:
   print("record/playback/direct argument needed")
   exit(10)

chrome_options = webdriver.ChromeOptions()
#chrome_options.add_argument("--proxy-server=%s" % "localhost:1234")
chrome_options.add_argument("--auto-open-devtools-for-tabs")

chromeWebDriverExePath = None

scriptArgs = sys.argv

for arg in scriptArgs[2:]:
    if arg.startswith("chromedriver="):
        chromeWebDriverExePath=arg[len("chromedriver="):]
if chromeWebDriverExePath is not None:
    chrome = webdriver.Chrome(executable_path=chromeWebDriverExePath, options=chrome_options)
else:
    chrome = webdriver.Chrome(options=chrome_options)
    
# url = "http://todo-backend-sinatra.herokuapp.com"
# time.sleep(5)

chrome.get("http://www.todobackend.com/specs/index.html?" + url + "/todos")
try:
    element = WebDriverWait(chrome, 20).until(
        EC.text_to_be_present_in_element((By.CLASS_NAME, "passes"), "16")
    )
    print("Compatibility suite: all 16 tests passed")

except TimeoutException as ex:
    print("Compatibility suite: did not finish with 16 passes. See open browser frame.")

# TODO warn that .NET process was not started.

print("mode: " + sys.argv[1])


if dotnet_process is not None:
    print("Killing Servirtium.NET")
    dotnet_process.communicate(input="x".encode("utf-8"))

print("Closing Selenium")
chrome.quit()
print("All done.")