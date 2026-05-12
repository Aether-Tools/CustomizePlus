# FAQ

### I need help with using Customize+!
Please ask your question at support discord server run by our community: [Aetherworks](https://discord.gg/KvGJCCnG8t). **Do not ask for help or support in issues section on GitHub.**

### How do I report a bug in Customize+ or leave a suggestion?
First we ask you join [Aetherworks Discord server](https://discord.gg/KvGJCCnG8t) and consult with community support team about your issue first. It's highly likely that you are not experiencing a bug in Customize+.

Before reporting bug or leaving a suggestion in this GitHub you need to **carefully** read [issue creation guidelines](https://github.com/Aether-Tools/CustomizePlus/issues/11).

### When will Customize+ be updated?
The updates are released when someone decides to contribute new feature or fix some bug. Please understand that real life and paid employment take priority over this project.

### IPC
Customize+ provides IPC for integrations with other plugins. We have opted for inline documentation, so please head over to `CustomizePlus/Api` directory and check source code files to see what kind of functionality is provided.

### I have received "Unsupported version of Customize+ configuration data detected." message.
This means your Customize+ configuration file is too old. You have 2 options:
* If you don't have any Customize+ data you care about
  * Go to plugin installer, right click on Customize+ and click "Reset plugin data and reload". This will reset Customize+ to clean state as if you have just installed it for the first time.
* If you have the data you care about and have been using Customize+ recently (aka your Templates and Profiles tabs are not empty)
  * Close the game. Open Windows Explorer and enter `%appdata%\XIVLauncher\pluginConfigs` in the address bar. Find `CustomizePlus.json` file in this folder and delete it. **This will reset Customize+ settings but keep Templates and Profiles intact.**
* If you have the data you care about and last time you have used Customize+ was before February 2024.
  * There is nothing you can do about this at this point. You need to reset your entire Customize+ settings by following steps in **If you don't have any Customize+ data you care about** option.

### Does Customize+ make backups of my data? / Is there a way to restore deleted template or profile?
Customize+ makes a full plugin data backup every time the game is launched and keeps last 15 copies of it.

To access backups close the game then open Windows Explorer and enter `%appdata%\XIVLauncher\backups\CustomizePlus` in the address bar.

To restore backups close the game then navigate to `%appdata%\XIVLauncher\pluginConfigs` folder, delete `CustomizePlus.json` file and `CustomizePlus` folder and then restore them from desired backup file.