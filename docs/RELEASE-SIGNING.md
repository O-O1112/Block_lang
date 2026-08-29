# Windows release signing

Block release artifacts always receive SHA-256 checksums and GitHub build
provenance attestations. Those controls detect replacement; they do not create a
trusted Windows publisher identity.

For an official signed installer, obtain an Authenticode code-signing certificate
for the project publisher and make it available in the Windows certificate store.
Then build with:

```powershell
.\build-release.ps1 -Version 2.2.6 -ReleaseDirectory .\release `
  -SigningCertificateThumbprint '<certificate thumbprint>'
```

The build uses SHA-256 file signing and a trusted timestamp service, then requires
the resulting installer signatures to validate. Never commit the private key or a
PFX password. Never distribute a self-signed development certificate as proof of
official publisher identity.

Before publishing, check both installer aliases:

```powershell
Get-AuthenticodeSignature .\release\BlockSetup-v2.2.6.exe
Get-AuthenticodeSignature .\release\BlockSetup.exe
```

Unsigned development builds must be described as unsigned. Users should compare
their SHA-256 values with `SHA256SUMS.txt` and verify GitHub attestations with:

```powershell
gh attestation verify .\BlockSetup-v2.2.6.exe -R O-O1112/Block_lang
```
