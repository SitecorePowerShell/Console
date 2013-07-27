-- About

This package is a Vista themed javascript progress bar. It supports both determinate mode (known progress) and indeterminate mode (unknown progress).

-- Quick Usage

Usage is as simple as including css file and the javascript class file.

<script type="text/javascript" src="progressbar.js"></script>

<link href="progressbar.css" rel="stylesheet" type="text/css" />

Then add a div to the page that you want to contain the javascript progress bar. It must have a unique id.

<div id="progress"></div>

With an onload handler or within script tags that appear after the div, instance the progress bar class. Send options to the constructor with an object.

<script type="text/javascript">var progressbar = new VistaProgressBar({id:'progress',width:300,highlight:true,smooth:true});</script>

The progressbar can now be controlled with several functions that are part of the VistaProgressBar object. If you have more than one progress bar, instance each progress bar to a seperate variable so that you can control each one idependently.

-- Options
 
All Options are passed in an object upon instancing the object.

  mode - Sets the way that the progress bar will work. Defualt: determinate. Options are either indeterminate or determinate.
  width - Sets the progress bar width. Defualt is 250px. Any pixel width can be used.

  Determinate mode options:
    smooth - Smooth progress transistion
    highlight - Turn on highlight. defualt: false
    highlightspeed - Speed in milliseconds for the highlight to traverse the bar. defualt: 1000ms
  
  Indeterminate mode options:
    highlightspeed - Speed in milliseconds for the highlight to traverse the bar. defualt: 1000ms 

-- Functions

Each mode has two functions.

determinate mode:
  addProgress(int) - Adds an amount of progress to the progress as it is currently, will stop when the total reaches 100.
  setProgress(int) - Sets the progress to a specifed integer. This can be 0-100.

indeterminate mode:
  start() - Starts the progress bar.
  stop() - Stops the progress bar.


-- Compatibility

Tested with and working with:
  Firefox 2.0
  Internet Explorer 7.0
  Safari 3.0 Beta
  Opera 9.0

  Internet Exploer 6.0 works, but it doesnt support PNG's with an alpha channel; the highlight or the indeterminate mode will not work correctly.

-- Support

No offical support is provided with this package. I would execpt that if you are using a this package you have at least a basic understanding of javascript. If you find bugs or have feature requests you may contact me with them at datasage@theworldisgrey.com.