MultiBloxy portable package
===========================

Requirements
------------
- 64-bit Windows supported by .NET 10
- An installed, unmodified Roblox client
- Standard user permissions; administrator rights are not required

Files
-----
- MultiBloxy.exe: self-contained portable application
- MultiBloxy.exe.sha256: checksum used to verify the executable
- release-manifest.json: version, source commit, target, and executable hash
- LICENSE.txt: MIT license

Configuration and logs
----------------------
Settings: %LOCALAPPDATA%\MultiBloxy\config.xml
Logs:     %LOCALAPPDATA%\MultiBloxy\logs\MultiBloxy.log

Security notice
---------------
MultiBloxy changes how the Roblox client coordinates multiple processes. Roblox
may block this behavior now or in the future. This project cannot guarantee that
the feature works or that using it is acceptable under every current Roblox rule.
Do not disable Windows security warnings merely because a download claims to be
safe. Prefer a signed release and verify MultiBloxy.exe.sha256.

This is a portable executable, not a Windows installer. It does not register an
uninstaller or add itself to startup.
