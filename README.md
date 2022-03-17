# PandaDoc Exporter
 A quick program I wrote for work to export answers from a website called PandaDoc.

## Some Background

 In the Summer of 2021, I served as a Business Intern for a 200+ employee sized company. Part of my responsibilities were to facilitate a smooth employee and manager review process for the entire company. For the reviews to be official and admissible in court they had to be electronically signed through a website similar to DocUSign. I ended up selecting PandaDoc to facilitate the reviews due to their cheaper offer. Part of the deal was that they would send and export all 400+ reviews into spreadsheets form. They successfully sent all the reviews and the entire company filled out their reviews (after some passive aggressive emails). However, near the end of the review cycle they told me that they could not successfully export the reviews. I took a look at their API system and, after they gave me a top-level integration key, I managed to scrape together a quick export program to create the spreadsheet system. That's what this is.

 ## The Program

The program takes on input, a CSV file. It has key information like keys and stuff in the same CSV that it exports too. This was to make it simpler to the user. The user provides a search tag for the files they are looking for and the program returns the number of files that match it in the official company account. The PandaDoc system has an API call limit of 200 per a minute, so a large part of the program was running a loop through as many files as possible and then waiting for the next API Call reset. The bulk of the program is splitting up the received data into various spreadsheet cells. This was a useful exercise in learning an unfamiliar API system in a timely fashion.
