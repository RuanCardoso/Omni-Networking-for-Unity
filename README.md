# OmniNet - Networking at the Subatomic Level in Unity

<table>
  <tr>
    <td width="200">
      <img src="icon.png" alt="OmniNet Logo" width="200" />
    </td>
    <td>
      <h3>OmniNet – High-Performance Multiplayer Framework for Unity</h3>
      <p>
        <strong>OmniNet</strong> is a Unity networking framework designed for performance-critical multiplayer game development. Built to handle real-time communication efficiently, it leverages low-level memory optimizations and modern .NET features to ensure minimal overhead and maximum scalability.
      </p>
      <p>
        <strong>Requirements:</strong> Unity 2021.3 or newer. This version introduced support for .NET Standard 2.1, enabling advanced features such as <code>Span</code>, <code>Memory</code>, and <code>ArrayPool</code>, which are extensively used in OmniNet for optimal performance.
      </p>
      <p>
        <strong>Documentation:</strong>  
        <a href="https://ruancardoso.github.io/Omni-Networking-Doc/">https://ruancardoso.github.io/Omni-Networking-Doc/</a>
      </p>
      <p><em>Developed by Ruan Cardoso and Antonio Junior</em></p>
    </td>
  </tr>
</table>

## 🚀 Installation

### ✅ Requirements

- Unity `2021.3` or newer
- API Compatibility Level: `.NET Standard 2.1+`
- Git installed (optional)

> ⚠️ For best results, use Unity LTS versions. Refer to the [Releases page](https://github.com/RuanCardoso/Omni-Networking-for-Unity/releases) for tested versions.

| Unity Version   | Supported |
|-----------------|-----------|
| 2021.3 LTS      | ✅        |
| 2022.3 LTS      | ✅        |
| 2023.2 (Beta)   | ✅        |
| 6000.0 LTS      | ✅        |

---

### 📦 Install via Package Manager (Recommended)

1. Open `Window > Package Manager`
2. Click the `+` button → `Add package from git URL`
3. Paste the following:
   ```
   https://github.com/RuanCardoso/Omni-Networking-for-Unity.git
   ```

---

### 🛠️ Manual Installation

1. Install required packages via Package Manager:
   ```
   com.unity.localization@1.5.4  
   com.unity.nuget.newtonsoft-json@3.2.1
   ```
2. Download the latest version from GitHub and copy the contents to your project’s `Assets` folder.

---

### ✅ Verify Installation

- No errors in the Console
- "Omni Networking" appears in Unity’s top menu
