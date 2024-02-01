This is a very simple code example of downloading tiled images from an image server, in this case archion.de.  

You pass the url of the PDF you are viewing on teh command line.  The program parses out the ID of the document from the URL and uses it to download the pages and then for each page, 
the tiles for the page image in the highest resolution.  Finally, the individual tiles are downloaded and composted into a singke large image.

The code contains examples of parsing strings with RegEx, downloading and parsing JSON files, composing an image from smaller images, and parallel loops for performance.

This example is limited on archion.de to the free samples posted on their website.  Testing with IDs of non-free documents returned an error 
and I don't have a subscription to investigate further and see how the paid PDF viewer operates.
