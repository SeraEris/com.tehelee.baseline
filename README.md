# Tehelee's Baseline

This package provides a lightweight networked gameplay baseline based on the LLAPI of Unity Transport.
It relies on class-based packet definitions and server-authoritative gameplay.

Additionally there are included utilities for runtime and editor usage, as well as quality-of-life runtime components.

## Installation

Using Unity's Package Manager ( in Unity 2019.1+ ) simply add this as a GIT-based package:
```
https://github.com/Tehelee/com.tehelee.baseline.git
```
![Package Manager - Add GIT Package](/.Github/PackageManager_Add-Through-GIT.png)

Additionally you can download and extract the zip to your project's Packages folder:
![Package Manager - Add ZIP Package](/.Github/PackageManager_Add-Through-Zip.png)

## Dependencies

These are automatically referenced by the package manager, but are listed here for posterity:
Package | Version
--------|--------
com.unity.burst | 1.2.3
com.unity.collections | 0.3.0-preview.0
com.unity.transport | 0.2.4-preview.0
com.unity.textmeshpro | 3.0.1