RockSmith CDLC Renamer
===================

Renames Rocksmith 2014 CDLC files to match the Custom Song Creator output format: Artist_Song-Name_Version

I was confounded by all the varied and seemingly random filenames from the various customs I've been downloading
for Rocksmith 2014, so I decided to use the Custom Song Creator method of naming DLC in my own simple(r) renamer. 
It also helps to identify songs that have screwy metadata so you can fix them using the Custom Song Creator.
This renamer ONLY WORKS FOR PC CDLC.

It's a simple console app at the moment, with no directory browsing or input parameters.  Just drop the CDLCRenamer.exe 
(located in the exe folder) into your Rocksmith 2014 DLC folder, and double-click to run it.  The program optionally
outputs a single Songs-<datestamp>.txt that lists all the files it renamed.

<strong>WARNING</strong>: This program will rename ALL valid .psarc files in the folder where it is run.  You might want to back up your songs before your first run in case you are not happy with the results.  It doesn't actually change any metadata, just the filenames.

What This Program Does
======================

The CDLC files store two sets of data for Song and Artist:<br>
  Song Title<br>
  Song Title Sort<br>
  Artist<br>
  Artist Sort<br>

The Sort fields determine where the song/artist appears within the game when you're browsing through the list of songs.
For this reason I use the Sort fields when renaming the files.  If, for example, you had a file for The Cure
song "A Forest", here's what it might look like:<br>
  <strong>Metadata</strong><br>
  Song Title: A Forest<br>
  Song Title Sort: Forest<br>
  Artist: The Cure<br>
  Artist Sort: Cure<br>

  Old wonky filename: TheCur-A-Forrest_CDLCByKen_v1_p.psarc<br>
  New filename: Cure_Forest_v1_DD_p.psarc<br>

And now you have a better, sortable, reliable filename.  

If the Sort metadata fields are wrong, i.e. they start with "A " or "The ", you'll see that reflected in the filename. 
You can fix metadata by using the "Import Package" button in the Custom Song Creator Toolkit, editing the metadata, 
and clicking the "Generate" button to create a new, fixed file.

Options
=======

You can modify the options.ini file to customize the separators in the filename.  For example, you may prefer an actual space instead of the "_" character.

<strong>I highly recommend backing up first, and experimenting on the backup set/subset of your files!</strong>
