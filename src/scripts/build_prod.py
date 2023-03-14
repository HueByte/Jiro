import os
import sys
from pathlib import Path


def getTargetOs(input: int) -> str:
    result = ""
    match input:
        case 1:
            result = "win-x64"
        case 2:
            result = "linux-x64"
        case 3:
            result = "osx-x64"
        case 4:
            result = "linux-arm64"
        case 5:
            result = "linux-arm32"
    return result


def getBuildCommand(inputPath: str, outputPath: str, target: str) -> str:
    return f'dotnet publish {inputPath} -c Release /p:DebugType=None /p:DebugSymbols=false -r {target} -o {outputPath}'


# Get the target OS
print("Target Operating system:")
print("1 Windows 64 bit")
print("2 Linux 64 bit")
print("3 MacOS 64 bit")
print("4 Linux ARM 64 bit")
print("5 Linux ARM 32 bit")

choice = input("Enter your choice: ")
target = getTargetOs(int(choice))

# Get paths
dirname, _ = os.path.split(os.path.abspath(__file__))
root = Path(dirname).parent
outputPath = os.path.join(root, 'build')

apiPath = os.path.join(root, 'Jiro.Kernel\Jiro.Api')
clientPath = os.path.join(root, 'Jiro.Client')
tokenizerPath = os.path.join(root, 'Jiro.TokenApi')

print("Root path", root)
print('Api Path', apiPath)
print('Client Path', clientPath)
print('Tokenizer Path', tokenizerPath)

if not os.path.exists(os.path.join(root, 'build')):
    os.mkdir(os.path.join(root, 'build'))

buildId = 0

# Build API
print("Building API")

apiBuildCommand = getBuildCommand(
    apiPath, os.path.join(outputPath, 'api'), target)

buildId = os.system(apiBuildCommand)

# Build Client
print("Building Client")

clientBuildCommand = getBuildCommand(
    clientPath, os.path.join(outputPath, 'web'), target)

buildId = os.system(clientBuildCommand)

# build Tokenizer
print("Building Tokenizer")

buildId = os.system('pip install pyinstaller')

buildId = os.system(
    f'pyinstaller -F {tokenizerPath}/main.py --hidden-import=tiktoken_ext.openai_public --hidden-import=tiktoken_ext')

print("Done!")
