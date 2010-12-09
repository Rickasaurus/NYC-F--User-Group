//
// This example is from Howard Mansell's 12-09-2010 NYC F# User Group Talk
// http://www.meetup.com/nyc-fsharp/calendar/15327380/
//

// Some references
 
#r "System.Xml.dll"
#r "System.Xml.Linq.dll"
 
#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml.dll"
 
open System
open System.Net
open System.Xml.Linq
open System.Windows
open System.Windows.Controls
open System.Windows.Media.Imaging
 
// Definitions of XML namespaces we will use
let xa = XNamespace.Get("http://www.w3.org/2005/Atom")
let xm = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")
let xd = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices")
 
// Data model for movies
type Movie = {
    Title: string
    Url: string
    BoxArtUrl: string
}
 
// Get the movies for a given year
let getMoviesFor year = async {
    let client = new WebClient()
    let query = sprintf "http://odata.netflix.com/Catalog/Titles?$filter=ReleaseYear eq %d&$top=100&$select=Url,BoxArt"
    let! xml = client.AsyncDownloadString(Uri(query year))
    let doc = XDocument.Parse xml
 
    return
        [|
            for entry in doc.Descendants(xa + "entry") ->
                let properties = entry.Element(xm + "properties")       
                {  
                    Title = entry.Element(xa + "title").Value
                    Url = properties.Element(xd + "Url").Value
                    BoxArtUrl = properties.Element(xd + "BoxArt").Element(xd + "LargeUrl").Value
                }
        |]
    }
 
// Refresh the movies in the panel
let refreshMovies (panel: Panel) year = async {
    // Run this on a background thread
    let! moviesAsync = Async.StartChild (getMoviesFor year)
    // Get the result
    let! movies = moviesAsync
 
    // Repopulate the panel with images
    panel.Children.Clear()
 
    for movie in movies do
        Image(Source = BitmapImage(Uri(movie.BoxArtUrl)),
              ToolTip = ToolTip(Content = movie.Title),
              Height = 150.0,
              Margin = Thickness(5.0))                                    
        |> panel.Children.Add
        |> ignore // Add returns a number...
}
 
// A panel for our input box and button
let topPanel = StackPanel(Orientation = Orientation.Horizontal)
 
// The wrap panel to populate with movie images
let wrapPanel = WrapPanel()
 
// Define the elements for our topPanel
let text = TextBlock(Text = "Release Year", Margin = Thickness(10.))
let yearBox = TextBox(Text = "2009", Margin = Thickness(10.))
let searchButton = Button(Content = "Search", Margin = Thickness(10.), IsDefault = true)
 
// Ensure search button is enabled only when year is valid
yearBox.TextChanged.Add(fun _ -> searchButton.IsEnabled <-
                                      match Int32.TryParse yearBox.Text with
                                      | true, year -> year >= 1900 && year <= 2099
                                      | _ -> false)
// Search button refreshes movies
searchButton.Click.Add(fun _ -> Async.StartImmediate(refreshMovies wrapPanel (Int32.Parse yearBox.Text)))
 
// Set up the layout
TextBlock.SetFontSize(topPanel, 20.0)
topPanel.Children.Add(text)
topPanel.Children.Add(yearBox)
topPanel.Children.Add(searchButton)
 
let wholePanel = StackPanel()
wholePanel.Children.Add(topPanel)
wholePanel.Children.Add(wrapPanel)
 
// Show the window
let window = new Window(Title = "Netflix Movie Finder", Content = ScrollViewer(Content = wholePanel))
[<STAThread>] ignore <| (new Application()).Run window