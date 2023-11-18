import os
import build_common.git as git
import build_common.utils as utils

NUGET_API_KEY = os.environ.get('NUGET_API_KEY')
#if (NUGET_API_KEY == None):
#    raise LookupError(f"Env. variable 'NUGET_API_KEY' is not set!")

artifactsDir = os.path.join(os.getcwd(), "artifacts")
if (not os.path.isdir(artifactsDir)):
    os.makedirs(artifactsDir)

branch = git.get_version_from_current_branch()
commitIndex = git.get_last_commit_index()
version = f"{branch}.{commitIndex}"

print(f"===========================================", flush=True)
print(f"Running tests: '{version}'", flush=True)
print(f"===========================================", flush=True)
utils.callThrowIfError("dotnet test")

print(f"===========================================", flush=True)
print(f"Packing nugets: '{version}'", flush=True)
print(f"===========================================", flush=True)
nugetArtifactsDir = os.path.join(artifactsDir, "nugets")
if (not os.path.isdir(nugetArtifactsDir)):
    os.makedirs(nugetArtifactsDir)
utils.callThrowIfError(f"dotnet pack -c Release /p:Version={version} -o \"{nugetArtifactsDir}\"")

print(f"===========================================", flush=True)
print(f"Pushing nugets: '{version}'", flush=True)
print(f"===========================================", flush=True)
utils.callThrowIfError(f"dotnet nuget push \"{nugetArtifactsDir}\*.nupkg\" --api-key {NUGET_API_KEY} --source https://api.nuget.org/v3/index.json")

print(f"===========================================", flush=True)
print(f"Creating tag: '{version}'", flush=True)
print(f"===========================================", flush=True)
git.create_tag_and_push(version)

print(f"===========================================", flush=True)
print(f"Merging with main branch...", flush=True)
print(f"===========================================", flush=True)
branchName = git.get_current_branch_name()
git.merge("main", branchName, True, "casualshammy")

print(f"===========================================", flush=True)
print(f"Done!", flush=True)
print(f"===========================================", flush=True)
