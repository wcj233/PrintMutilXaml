using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Printing;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PrintMutipleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private double ApplicationContentMarginLeft = 0.075;
        private double ApplicationContentMarginTop = 0.03;
        private PrintDocument printDocument;
        private IPrintDocumentSource printDocumentSource;

        /// <summary>
        /// A list of UIElements used to store the print preview pages.  This gives easy access
        /// to any desired preview page.
        /// </summary>
        internal List<UIElement> printPreviewPages;
        private List<UIElement> AllPrintingPages;

        // Event callback which is called after print preview pages are generated.  Photos scenario uses this to do filtering of preview pages
        private event EventHandler PreviewPagesCreated;


        public MainPage()
        {
            this.InitializeComponent();
            printPreviewPages = new List<UIElement>();
            AllPrintingPages = new List<UIElement>();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            RegisterForPrinting();
            PreparePrintContent(new BlankPage2());
            PreparePrintContent(new BlankPage2());
            await PrintManager.ShowPrintUIAsync();
        }


        public void RegisterForPrinting()
        {
            printDocument = new PrintDocument();
            printDocumentSource = printDocument.DocumentSource;
            printDocument.Paginate += CreatePrintPreviewPages;
            printDocument.GetPreviewPage += GetPrintPreviewPage;
            printDocument.AddPages += AddPrintPages;

            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += PrintTaskRequested;
        }

        /// <summary>
        /// This function unregisters the app for printing with Windows.
        /// </summary>
        public void UnregisterForPrinting()
        {
            if (printDocument == null)
            {
                return;
            }

            printDocument.Paginate -= CreatePrintPreviewPages;
            printDocument.GetPreviewPage -= GetPrintPreviewPage;
            printDocument.AddPages -= AddPrintPages;

            // Remove the handler for printing initialization.
            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested -= PrintTaskRequested;

            PrintCanvas.Children.Clear();
        }

        public void PreparePrintContent(FrameworkElement page)
        {
            PrintCanvas.Children.Add(page);
            AllPrintingPages.Add(page);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();
        }


        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;
            printTask = e.Request.CreatePrintTask("Sample", sourceRequested =>
            {
                // Print Task event handler is invoked when the print job is completed.
                printTask.Completed += async (s, args) =>
                {
                    // Notify the user when the print operation fails.
                    if (args.Completion == PrintTaskCompletion.Failed)
                    {

                    }
                };

                sourceRequested.SetSource(printDocumentSource);
            });
        }


        private void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            lock (printPreviewPages)
            {
                // Clear the cache of preview pages
                printPreviewPages.Clear();

                // Clear the print canvas of preview pages
                PrintCanvas.Children.Clear();

                for (int i = 0; i < AllPrintingPages.Count; i++)
                {

                    RichTextBlockOverflow lastRTBOOnPage;

                    // Get the PrintTaskOptions
                    PrintTaskOptions printingOptions = ((PrintTaskOptions)e.PrintTaskOptions);

                    // Get the page description to deterimine how big the page is
                    PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);

                    // We know there is at least one page to be printed. passing null as the first parameter to
                    // AddOnePrintPreviewPage tells the function to add the first page.
                    lastRTBOOnPage = AddOnePrintPreviewPage(null, pageDescription, i);

                    // We know there are more pages to be added as long as the last RichTextBoxOverflow added to a print preview
                    // page has extra content
                    while (lastRTBOOnPage.HasOverflowContent && lastRTBOOnPage.Visibility == Windows.UI.Xaml.Visibility.Visible)
                    {
                        lastRTBOOnPage = AddOnePrintPreviewPage(lastRTBOOnPage, pageDescription, 0);
                    }
                }

                if (PreviewPagesCreated != null)
                {
                    PreviewPagesCreated.Invoke(printPreviewPages, null);
                }

                PrintDocument printDoc = (PrintDocument)sender;

                // Report the number of preview pages created
                printDoc.SetPreviewPageCount(printPreviewPages.Count, PreviewPageCountType.Intermediate);
            }
        }


        private void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            printDoc.SetPreviewPage(e.PageNumber, printPreviewPages[e.PageNumber - 1]);
        }


        private void AddPrintPages(object sender, AddPagesEventArgs e)
        {
            // Loop over all of the preview pages and add each one to  add each page to be printied
            for (int i = 0; i < printPreviewPages.Count; i++)
            {
                // We should have all pages ready at this point...
                printDocument.AddPage(printPreviewPages[i]);
            }

            PrintDocument printDoc = (PrintDocument)sender;

            // Indicate that all of the print pages have been provided
            printDoc.AddPagesComplete();
        }

        private RichTextBlockOverflow AddOnePrintPreviewPage(RichTextBlockOverflow lastRTBOAdded, PrintPageDescription printPageDescription, int index)
        {

            //// XAML element that is used to represent to "printing page"
            FrameworkElement page;

            // The link container for text overflowing in this page
            RichTextBlockOverflow textLink;

            // Check if this is the first page ( no previous RichTextBlockOverflow)
            if (lastRTBOAdded == null)
            {
                // If this is the first page add the specific scenario content
                page = AllPrintingPages[index] as FrameworkElement;
            }
            else
            {
                // Flow content (text) from previous pages
                page = new BlankPage1(lastRTBOAdded);
            }


            page.Width = printPageDescription.PageSize.Width;
            page.Height = printPageDescription.PageSize.Height;

            // Get the margins size
            // If the ImageableRect is smaller than the app provided margins use the ImageableRect
            double marginWidth = Math.Max(printPageDescription.PageSize.Width - printPageDescription.ImageableRect.Width, printPageDescription.PageSize.Width * ApplicationContentMarginLeft * 2);
            double marginHeight = Math.Max(printPageDescription.PageSize.Height - printPageDescription.ImageableRect.Height, printPageDescription.PageSize.Height * ApplicationContentMarginTop * 2);

            if (lastRTBOAdded == null)
            {

                switch (index)
                {
                    case 0:

                        // Set-up "printable area" on the "paper"
                        printPreviewPages.Add((Grid)page.FindName("MyGrid1"));
                        Grid printableArea1 = (Grid)page.FindName("MyGrid1");
                        printableArea1.Width = page.Width - marginWidth;
                        printableArea1.Height = page.Height - marginHeight;



                        break;
                    case 1:
                        StackPanel printableArea = (StackPanel)page.FindName("MyGrid2");
                        printableArea.Width = page.Width - marginWidth;
                        printableArea.Height = page.Height - marginHeight;
                        printPreviewPages.Add((StackPanel)page.FindName("MyGrid2"));

                        break;
                }
            }
            else //Footer
            {
                Grid printableArea = (Grid)page.FindName("PrintableArea");
                printableArea.Width = page.Width - marginWidth;
                printableArea.Height = page.Height - marginHeight;
                printPreviewPages.Add(page);
            }



            PrintCanvas.Children.Add(page);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();

            // Find the last text container and see if the content is overflowing
            textLink = (RichTextBlockOverflow)page.FindName("ContinuationPageLinkedContainer");


            return textLink;
        }
    }
        
    
}
