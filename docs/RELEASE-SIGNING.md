# Windows release signing

Block release artifacts always receive SHA-256 checksums and GitHub build
provenance attestations. Those controls detect replacement; they do not create a
trusted Windows publisher identity. See the project [code signing
policy](CODE-SIGNING-POLICY.md) for the required release controls.

For an official signed installer, obtain an Authenticode code-signing certificate
for the project publisher and make it available in the Windows certificate store.
Then build with:

```powershell
.\build-release.ps1 -Version 2.2.6.5 -ReleaseDirectory .\release `
  -SigningCertificateThumbprint '<certificate thumbprint>'
```

The build signs the Lite, Standard, and Block+ executables before their ZIP
packages are created, then signs the versioned installer and copies it to the
stable installer alias. Release verification requires every published Windows
executable to have a valid signature. Never commit the private key or a PFX
password. Never distribute a self-signed development certificate as proof of
official publisher identity.

Before publishing, check both installer aliases:

```powershell
Get-AuthenticodeSignature .\release\BlockSetup-vX.Y.Z.exe
Get-AuthenticodeSignature .\release\block.exe
Get-AuthenticodeSignature .\release\block-lite.exe
Get-AuthenticodeSignature .\release\block-plus.exe
```

Unsigned development builds must be described as unsigned. Users should compare
their SHA-256 values with `SHA256SUMS.txt` and verify GitHub attestations with:

```powershell
gh attestation verify .\BlockSetup-vX.Y.Z.exe -R O-O1112/Block_lang
```
