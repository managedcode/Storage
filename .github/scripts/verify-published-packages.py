"""Verify every packed NuGet artifact against the public feed before declaring a release."""
import argparse
import io
import re
import time
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


def metadata(package_bytes):
    with zipfile.ZipFile(io.BytesIO(package_bytes)) as archive:
        names = [name for name in archive.namelist() if name.endswith(".nuspec")]
        if len(names) != 1:
            raise ValueError("Package must contain one nuspec")
        root = ET.fromstring(archive.read(names[0]))
    package = root.find("{*}metadata")
    repository = package.find("{*}repository")
    return (package.findtext("{*}id"), package.findtext("{*}version"),
            repository.get("commit") if repository is not None else None)


def verify(directory, commit, timeout):
    packages = sorted(directory.glob("*.nupkg"))
    if not packages:
        raise ValueError("No packages to verify")
    pending = {}
    for package in packages:
        identity = metadata(package.read_bytes())
        package_id, version, source_commit = identity
        if not re.fullmatch(r"ManagedCode\.Storage(?:\.[A-Za-z0-9_.-]+)?", package_id):
            raise ValueError(f"Unexpected package identity: {package_id}")
        if source_commit != commit:
            raise ValueError(f"{package_id}: packed repository commit does not match the release commit")
        pending[package_id] = identity
    deadline = time.monotonic() + timeout
    while pending:
        for package_id, identity in list(pending.items()):
            _, version, _ = identity
            name = package_id.lower()
            url = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg"
            try:
                response = subprocess.run(["curl", "--fail", "--silent", "--show-error", "--max-time", "15",
                                           "--header", "Cache-Control: no-cache", url],
                                          check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=20)
                published = metadata(response.stdout)
                if published != identity:
                    raise ValueError(f"{package_id}: published identity/version/commit differs from the packed artifact")
                print(f"Verified {package_id} {version} at {commit}", flush=True)
                del pending[package_id]
            except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as error:
                print(f"Waiting for {package_id}: {type(error).__name__}", flush=True)
        if not pending:
            break
        if time.monotonic() >= deadline:
            raise TimeoutError("Packages unavailable before the publication deadline: " + ", ".join(pending))
        time.sleep(min(20, max(0, deadline - time.monotonic())))
    print(f"Verified all {len(packages)} published packages", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("commit")
    parser.add_argument("--timeout-seconds", type=int, default=600)
    arguments = parser.parse_args()
    verify(arguments.directory, arguments.commit, arguments.timeout_seconds)
