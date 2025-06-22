
![excl_diagram_engaging](https://github.com/user-attachments/assets/631260be-2283-43cb-8bd2-8aba7e2ea802)



# Exclinjector

A custom C# tool for stealthy payload execution on Windows, with built-in Defender bypass and UAC elevation.

---

## Overview

**ExclInjector** performs the following steps, in order, to ensure your payload runs undetected:

1. **Anti-analysis checks**  
   – Detects debuggers, low CPU count, small disk size or generic usernames to avoid sandboxes/VMs  
2. **COM-based UAC elevation**  
   – Elevates to admin via a silent COM prompt if not already elevated  
3. **WMI-driven Defender exclusion**  
   – Uses `MSFT_MpPreference` to add and verify a Defender exclusion for your payload path  
4. **Stealth payload launch**  
   – Once the environment is safe, injects or executes your payload from `%TEMP%` with no visible UI

---

## Features

- No disk artifacts: payload lives entirely in `%TEMP%`  
- Robust anti-analysis: avoids common sandbox/VM heuristics  
- Silent UAC elevation: no intrusive prompts or visible console windows  
- Automatic Defender exclusion: keeps your payload off Defender’s radar  
- Easy integration: call `PrepareEnvironment(string path)` then plug in any injector (shellcode, DLL, etc.)

---

## Requirements

- **Windows 10 / 11** (x64)  
- **.NET Framework 4.7.2+** or **.NET 5+**  
- Administrative privileges (the tool will elevate itself if needed)

---

## Building

1. **Clone the repo**  
   ```bash
   git clone https://github.com/callsimba/ExclInjector.git
   cd ExclInjector
