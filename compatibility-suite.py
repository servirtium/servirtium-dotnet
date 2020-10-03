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

if len(sys.argv) > 1:
   if sys.argv[1] == "record":
       # TODO check that .NET process is already started.
       url = "http://localhost:1234"
       dotnet_process = subprocess.Popen(["dotnet", "run", "--project", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj", "--", "record", "--urls=http://*:1234"])
       print(".NET process: "+str(dotnet_process.pid))
   elif sys.argv[1] == "playback":
       url = "http://localhost:1234"
       dotnet_process = subprocess.Popen(["dotnet", "run", "--project", "./Servirtium.StandaloneServer/Servirtium.StandaloneServer.csproj", "--", "playback", "--urls=http://*:1234"])
       print(".NET process: "+str(dotnet_process.pid))
   elif sys.argv[1] == "direct":
       print("showing reference Sinatra app online without Servirtium in the middle")
       url = "https://todo-backend-sinatra.herokuapp.com"
   else:
       print("Second arg should be record or playback")
       exit(10)
else:
   print("record/playback/direct argument needed")
   exit(10)
driver = webdriver.Chrome("D:/Tools/chromedriver.exe")

# time.sleep(5)

driver.get("https://www.todobackend.com/specs/index.html?" + url + "/todos")
try:
    element = WebDriverWait(driver, 300).until(
        EC.text_to_be_present_in_element((By.CLASS_NAME, "passes"), "16")
    )
    print("Compatibility suite: all 16 tests passed")

except TimeoutException as ex:
    print("Compatibility suite: did not finish with 16 passes. See open browser frame.")

# TODO warn that .NET process was not started.

print("mode: " + sys.argv[1])


if dotnet_process is not None:
    print("Killing Servirtium.NET")
    if platform.system() == "Windows":
        os.kill(dotnet_process.pid, signal.CTRL_C_EVENT)
    else:
        os.killpg(os.getpgid(dotnet_process.pid), signal.SIGTERM)
    dotnet_process.kill()

print("Closing Selenium")
driver.quit()
print("All done.")