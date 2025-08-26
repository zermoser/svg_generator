using PuppeteerSharp;
using System.Threading.Tasks;

namespace TimelineChartAPI.Services
{
    public interface IPngRenderingService
    {
        Task<byte[]> RenderSvgToPngAsync(string svgContent, int width, int height);
    }

    public class PngRenderingService : IPngRenderingService
    {
        public async Task<byte[]> RenderSvgToPngAsync(string svgContent, int width, int height)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            var html = CreateHtmlTemplate(svgContent, width, height);
            await page.SetContentAsync(html);

            var element = await page.QuerySelectorAsync("svg");
            return await element.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                OmitBackground = false
            });
        }

        private static string CreateHtmlTemplate(string svgContent, int width, int height)
        {
            return $@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ margin:0; padding:0; }}
                        svg {{ width:{width}px; height:{height}px; }}
                    </style>
                </head>
                <body>
                    {svgContent}
                </body>
                </html>";
        }
    }
}
