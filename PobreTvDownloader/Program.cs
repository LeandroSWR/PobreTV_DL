using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using SeleniumExtras.WaitHelpers;
using System.Diagnostics;
using OpenQA.Selenium.Support.UI;

namespace PobreTvDownloader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // https://www3.pobre.wtf/tvshows/tt0391666

            //Console.WriteLine("Insert Link:");

            string pattern = "<video.*?src=\"(.*?)\"";
            string parsedInfo = "";
            string? original = "https://www3.pobre.wtf/tvshows/tt0391666";

            //do
            //{
            //    original = Console.ReadLine();
            //} while (original == null || !original.Contains("https://"));

            string delimiter = "/";

            // Split the original string using the delimiter.
            string[] parts = original.Split(delimiter);

            // Get the part you want (in this case, the last part).
            parsedInfo = parts[parts.Length - 2] + "/" + parts[parts.Length - 1];

            // Load the default profile and set the volume to 0 and firefox to headless
            FirefoxOptions options = new FirefoxOptions();
            options.Profile = new FirefoxProfile("profile.default");
            options.Profile.SetPreference("media.volume_scale", "0.0");
            //options.AddArguments("--headless");
            
            // Create a new instance of the FirefoxDriver class.
            FirefoxDriver driver = new FirefoxDriver(options);

            // Navigate to the web page you want to load.
            driver.Navigate().GoToUrl(original);

            // Get the result of the download.
            string pageSource = driver.PageSource;

            // Use a regular expression to find the links in the source code.
            MatchCollection matches = Regex.Matches(pageSource, @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1");

            // Iterate over the matches and add each link to a list.
            List<string> seasonLinks = new List<string>();
            List<string> episodeLinks = new List<string>();
            foreach (Match match in matches)
            {
                if (match.Groups[2].Value.Contains(parsedInfo) && !match.Groups[2].Value.Contains("?page"))
                {
                    if (match.Groups[2].Value.Contains("episode"))
                    {
                        episodeLinks.Add(match.Groups[2].Value);
                    }
                    else
                    {
                        seasonLinks.Add(match.Groups[2].Value);
                    }
                }
            }

            for (int s = 1; s <= seasonLinks.Count; s++)
            {
                for (int e = 0; e < episodeLinks.Count; e++)
                {
                    if (s == 1 && e < 65)
                        continue;

                    // Navigate to the web page you want to load.
                    driver.Navigate().GoToUrl(episodeLinks[e]);

                    // Wait for the page to fully load by waiting for the document to be ready.
                    Thread.Sleep(4000);

                    // Get the element as a button
                    IWebElement button = driver.FindElement(By.CssSelector(".prePlaybutton"));

                    // Click the button.
                    button.Click();

                    //button = driver.FindElement(By.CssSelector("#vpBar > div.players > div:nth-child(2)"));

                    Thread.Sleep(8000);

                    // Find the iframe by its CSS selector.
                    var iframe = driver.FindElements(By.CssSelector("#embed > iframe"));

                    // If we have no iFrame wait for user to solve captcha
                    while (iframe.Count == 0)
                    {
                        // Create a new instance of the WebDriverWait class and set the timeout to 10 seconds.
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                        // Define the CSS selector for the CAPTCHA element.
                        string captchaCssSelector = "#mve > div.recaptcha";

                        // Wait for the CAPTCHA element to become invisible on the page.
                        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(captchaCssSelector)));
                        
                        // Wait for the page to fully load by waiting for the document to be ready.
                        Thread.Sleep(2000);

                        iframe = driver.FindElements(By.CssSelector("#embed > iframe"));

                        if (iframe.Count != 0)
                            break;

                        // Refresh the page.
                        driver.Navigate().Refresh();

                        // Wait for the page to fully load by waiting for the document to be ready.
                        Thread.Sleep(4000);

                        button = driver.FindElement(By.CssSelector(".prePlaybutton"));

                        // Click the button.
                        button.Click();

                        Thread.Sleep(10000);

                        // Find the iframe by its CSS selector.
                        iframe = driver.FindElements(By.CssSelector("#embed > iframe"));
                    }

                    // Switch to the iframe.
                    driver.SwitchTo().Frame(iframe[0]);

                    // Get the source of the iframe.
                    string iframeSource = driver.PageSource;

                    // Use a regular expression to find the links in the source code.
                    matches = Regex.Matches(iframeSource, pattern);

                    Console.WriteLine(e + ": " + matches[0].Groups[1].Value);

                    if (matches[0].Groups[1].Value.Contains("adVideo"))
                    {
                        Thread.Sleep(20000);
                        e--;

                        // Refresh the page.
                        driver.Navigate().Refresh();

                        continue;
                    }

                    // Define the arguments to pass to yt-dlp.
                    string arguments = $"--allow-u --downloader aria2c -f \"best[ext=mp4]\" bv,ba -o \"F:\\Series_3\\Morangos com Açucar\\Season {s}\\Morangos.com.Acucar.S0{s}.E{e + 1:000}.%(ext)s\" \"{matches[0].Groups[1].Value}\"";

                    // Create a new ProcessStartInfo instance with the path to yt-dlp and the arguments.
                    ProcessStartInfo startInfo = new ProcessStartInfo("yt-dlp.exe", arguments);

                    // Set the UseShellExecute property to false to run yt-dlp in the background.
                    startInfo.UseShellExecute = false;

                    // Create a new Process instance and start it with the specified ProcessStartInfo.
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.Start();

                    process.WaitForExit();
                }

                // Load next season

                if (s == seasonLinks.Count)
                {
                    break;
                }

                // Navigate to the web page you want to load.
                driver.Navigate().GoToUrl(seasonLinks[s]);

                // Get the result of the download.
                pageSource = driver.PageSource;

                // Use a regular expression to find the links in the source code.
                matches = Regex.Matches(pageSource, @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1");

                // Iterate over the matches and add each link to a list.
                episodeLinks.Clear();
                foreach (Match match in matches)
                {
                    if (match.Groups[2].Value.Contains(parsedInfo) && !match.Groups[2].Value.Contains("?page"))
                    {
                        if (match.Groups[2].Value.Contains("episode"))
                        {
                            episodeLinks.Add(match.Groups[2].Value);
                        }
                    }
                }
            }
        }
    }
}