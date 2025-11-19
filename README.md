# Customize+
Customize+ is a Dalamud plugin designed to give you better control over your Final Fantasy XIV character appearance. Namely it allows you to apply character bone manipulations during gameplay.

## Installing
**Do not use repo.json from this repository**

Add [Aether Tools](https://github.com/Aether-Tools/DalamudPlugins) or [Sea of Stars](https://github.com/Ottermandias/SeaOfStars) dalamud repository by following instructions on the respective pages. 

Then search for Customize+ in the plugin manager.

## FAQ

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

## Contributing
**This is important, please read before contributing.**

We believe that letting community help develop the plugin greatly benefits it, but we have several things which we would like everyone to be aware of:

* Since Customize+ is one of the major plugins in Dalamud ecosystem some parts of the code should be modified with big care.
	* We suggest you to consult with fellow community developers over at Aetherworks Discord server before changing anything related to plugin's core functionality and modifying existing IPC endpoints.
* Our main priority is to not introduce breaking changes unless absolutely necessary. That includes implementing things in a future-proof way when possible. Non-transferrable data (settings, templates and profiles) should be preserved when upgrading to next plugin release no matter the changes in data structures.
* Since a lot of our UI code is based on Glamourer and OtterGui, we are trying to to keep our UI implementation in line with the way UI is implemented in Glamourer. This gives us an advantage of occasionally getting new UI functionality without doing much changes on our side, but requires us to periodically check Glamourer code for changes in the UI code.
* We expect contributed code to adhere to the code style of already existing code and be reasonably well tested (i.e. not break existing functionality and, well, actually work). We kindly ask you to be ready to make changes to the code in your Pull Requests if we feel like it needs to be changed.
* Vibe coded or any other contributions where it is suspected that AI use was considerably high and without much oversight and/or understanding of Customize+ code are highly discouraged and might be rejected.

## Development team
* [Risa](https://github.com/RisaDev/) - Author and Lead Developer.

## Acknowledgements
* User interface and general plugin architecture is heavily based on the code written as a part of [Glamourer](https://github.com/Ottermandias/Glamourer) and [OtterGui](https://github.com/Ottermandias/OtterGui/) projects. Original code is licensed under Apache License 2.0.
* Some of the game object interaction code is copied from [Penumbra](https://github.com/xivdev/Penumbra) and [Glamourer](https://github.com/Ottermandias/Glamourer) projects in order to make Customize+ not rely on Penumbra being installed.
* GitHub workflows have been taken from [Glamourer](https://github.com/Ottermandias/Glamourer) project.
* Customize+ is using code from [ECommons](https://github.com/NightmareXIV/ECommons) library for IPC functionality.
* Some of the bone manipulation code was taken from [Ktisis](https://github.com/ktisis-tools/Ktisis).
* Thanks to [Dalamud](https://github.com/goatcorp/Dalamud) team for making plugins possible at all.
* Special thanks goes to Yuki, Phenrei, Stoia, dendr01d and others for developing and maintaining original (1.0) version of Customize+.
* Thank you to everyone who contributed new features and bug fixes through pull requests.

## License
All files in this repository are licensed under the license listed in LICENSE.md file unless stated otherwise. By contributing the code into this repository you agreeing with licensing submitted code under this license.

*Customize+ includes code previously licensed to it by its contributors under MIT license. The text of the MIT license can be found [here](https://opensource.org/license/mit/). This code has been relicensed under license listed in LICENSE.md file.*

##### Final Fantasy XIV © SQUARE ENIX CO., LTD. All Rights Reserved. Customize+ and its developers are not affiliated with SQUARE ENIX CO., LTD. in any way.
