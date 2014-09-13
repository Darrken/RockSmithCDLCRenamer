RockSmithDLCRenamer
===================

Renames Rocksmith 2014 DLC files to match the Custom Song Creator output format: Artist_Song-Name_Version

I got sick of all the varied and seemingly random filenames from the various custom DLC I've been downloading
for Rocksmith 2014, so I decided to use the Custom Song Creator method of naming DLC in my own simple renamer. 
It also helps to identify songs that have screwy metadata so you can fix them using the Custom Song Creator.
This renamer ONLY WORKS FOR PC DLC.

It's a simple console app at the moment, with no directory browsing or inputs.  Just drop the DLCRenamer.exe 
(located in the exe folder) into your Rocksmith 2014 DLC folder, and double-click to run it.  The program
outputs a single Songs.txt that lists all the files it renamed.

<strong>WARNING</strong>: This program will rename ALL valid .psarc files in your DLC folder.  You might want to back up your DLC
folder before your first run in case you are not happy with the results.  It doesn't actually change any metadata,
just the filenames.

What This Program Does
======================

The DLC files store two sets of data for Song and Artist:<br>
  Song Title<br>
  Song Title Sort<br>
  Artist<br>
  Artist Sort<br>

The Sort fields determine where the song/artist appears within the game when you're browsing through the list of songs.
For this reason I use the Sort fields when renaming the files.  If, for example, you had a DLC file for The Cure
song "A Forest", here's what it might look like:<br>
  <strong>Metadata</strong><br>
  Song Title: A Forest<br>
  Song Title Sort: Forest<br>
  Artist: The Cure<br>
  Artist Sort: Cure<br>

  Old wonky filename: TheCur-A-Forrest_CDLCByKen_v1_DD_p.psarc<br>
  New filename: Cure_Forest_v1_DD_p.psarc<br>

Note that I try to preserve any existing "version" in the filename by keeping anything from _v# to the end of the
filename.  And now you have a better, sortable, reliable filename.  If the Sort metadata fields are wrong, i.e. 
they start with "A " or "The ", you'll see that reflected in the filename.

You can fix metadata by using the "Import Package" button in the Custom Song Creator Toolkit, editing the metadata, 
and clicking the "Generate" button.
