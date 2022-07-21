namespace Ecng.Tests.Net
{
	using System.Threading.Tasks;

	using Ecng.Net;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MjmlTests
	{
		[TestMethod]
		public async Task Render()
		{
			var client = new MjmlRestClient(Config.HttpClient, "c0f87ae2-20c1-48db-b6d9-fc4369002099", "7033a07b-d834-429a-894a-4a6ad7b3e415");

			var response = await client.RenderAsync(@"<mjml>
  <mj-body>
    <mj-container>
      <mj-section>
        <mj-column>
          <mj-text>Hello World!</mj-text>
        </mj-column>
      </mj-section>
    </mj-container>
  </mj-body>
</mjml>", default);

			(response.Html.Length > 1000).AssertTrue();
		}
	}
}
