# SCSM - Advanced Action Log Notifier
This management pack for SCSM 2016+ provides Action Log notifications on Incidents and Service Requests featuring integration into Azure Translation services.

In order to use Azure Translation services, an Azure subscription with a deployed Text Translation service must be deployed. Users then synced from Active Directory must have a defined Locale set on their User Object within the SCSM Console. It's worth noting that since Azure Translation services uses two letter language codes (e.g. EN = English, ES = Spanish) that when it comes to setting a User's Locale in SCSM; to Azure Translate, English (United States) and English (United Kingdom) are technically the same thing as they both use the identical language code of "EN".

Take the following examples:
1. The Affected User and the Assigned To user speak different languages and have different defined Locales (Spanish and English respectively) When the Affected User leaves a Comment, the Comment is submitted to Azure Translate to determine the language (i.e. Spanish). A simple comparison sees that EN does not equal ES. The Comment is then Translated to English for the Assigned To User, a new entry is made on the Action Log Entered By "Azure Translate/AFFECTED USER" and the Assigned To is notified of this new Comment. This same logic applies when the Assigned To leaves a Comment.
2. One of the Users on a Work Item doesn't have a defined Locale. No translation is performed and the respective User is notified of the Comment.
3. Neither of the Users on a Work Item have a defined Locale. No translation is performed and the respective User is notifed of the Comment.


<p align="center">
  <img src="/screenshots/adminui.png">
</p>
