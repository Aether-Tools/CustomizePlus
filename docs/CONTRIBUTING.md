# Contributing
We believe that letting community help develop the plugin greatly benefits it, but we have several things which we would like everyone to be aware of:

* Since Customize+ is one of the major plugins in Dalamud ecosystem some parts of the code should be modified with big care.
	* We suggest you to consult with fellow community developers over at Aetherworks Discord server before changing anything related to plugin's core functionality and modifying existing IPC endpoints.
* Our main priority is to not introduce breaking changes unless absolutely necessary. That includes implementing things in a future-proof way when possible. Non-transferrable data (settings, templates and profiles) should be preserved when upgrading to next plugin release no matter the changes in data structures.
* Since a lot of our UI code is based on Glamourer and OtterGui, we are trying to to keep our UI implementation in line with the way UI is implemented in Glamourer. This gives us an advantage of occasionally getting new UI functionality without doing much changes on our side, but requires us to periodically check Glamourer code for changes in the UI code.
* We expect contributed code to adhere to the code style of already existing code and be reasonably well tested (i.e. not break existing functionality and, well, actually work). We kindly ask you to be ready to make changes to the code in your Pull Requests if we feel like it needs to be changed.

# AI Usage Policies
Use of AI tools for Customize+ contributions is permitted under specific conditions listed below:

* Contributors are expected to fully own and understand all code they submit. The changes should be personally tested before submission.
* The level of AI use beyond basic autocomplete should be disclosed in the PR. Please read further for the details.
* Any communication with the team — including code, code comments, and GitHub comments — must come from the human contributor, not an AI agent acting autonomously. Pull requests opened by AI agents or automated tools will be rejected.

## AI Usage Disclosure
If AI was used at any point beyond basic autocomplete, disclose the level of AI involvement in your pull request description. We use the following levels, adapted from [AI-DECLARATION.md](https://ai-declaration.md/):

* **None:** No AI tools were used at any point. _You do not need to disclose this level._
* **Hint:** AI autocomplete or inline suggestions only. The human writes all code; AI occasionally completes a line or block. _You do not need to disclose this level._
* **Assist:** Human-led. AI is used on demand for specific tasks (generating a function, explaining code) but does not drive the work.
* **Pair:** Active human-AI collaboration throughout. Contribution is roughly equal.
* **Copilot:** AI implements while the human plans and reviews. The human defines what to build and validates the output, but the AI does most of the writing.
* **Auto:** AI acts autonomously with minimal human direction. The human may steer at a high level or approve outcomes, but does not write or closely direct the code.

If you did not use AI, or only used basic autocomplete/inline suggestions, you do not need to disclose anything.

## Enforcement

* **Entirely AI-generated contributions** with no meaningful human involvement will be rejected. Doing this twice will result in a ban in Aether-Tools repositories.
* **Undisclosed AI use** in a demonstrably AI-written submission will result in a ban in Aether-Tools repositories.
* **Understatement of the reported level of AI participation** will be treated the same as **Undisclosed AI use**.
* **Fixable issues:** If your submission shows AI-generated mistakes but clear human intent, we'll close it with an opportunity to fix and resubmit.

# Final words
We ask you to respect our time and limited resources, please don't waste them on contributions which break any of the rules and guidelines in this file.