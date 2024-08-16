using System.Net;
using System.Net.Sockets;
using DnsClient;
using Microsoft.AspNetCore.Mvc;

namespace TelnetSample.Controllers;
[Route("api/[controller]")]
[ApiController]
public class TelnetController : ControllerBase
{
	/// <summary>
	/// Gets the telnet asynchronous.
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="lookupClient">The lookup client.</param>
	/// <param name="tcpClient">The TCP client.</param>
	/// <param name="domain">The domain.</param>
	/// <param name="queryType">Type of the query.</param>
	/// <param name="port">The port.</param>
	/// <returns></returns>
	/// <exception cref="System.ArgumentException"></exception>
	[HttpGet("{domain}")]
	public async ValueTask<string> GetTelnetAsync(
		[FromServices] ILogger<TelnetController> logger,
		[FromServices] ILookupClient lookupClient,
		[FromServices] TcpClient tcpClient,
		string domain = "google.com",
		[FromQuery] string queryType = "A",
		[FromQuery] int port = 443)
	{
		try
		{
			var result = await lookupClient.QueryAsync(
				query: domain,
				queryType: (QueryType)Enum.Parse(typeof(QueryType), queryType),
				cancellationToken: HttpContext.RequestAborted)
				.ConfigureAwait(false);

			if (!IPAddress.TryParse(result.Answers.ARecords()?.FirstOrDefault()?.Address.ToString(), out var ipAddress))
				throw new ArgumentException(string.Format("{0} is not a valid IP address.", domain));

			await tcpClient.ConnectAsync(ipAddress.ToString(), port)
				.ConfigureAwait(false);

			var timeoutCount = 0;
			var timeoutMaxCount = 15;
			do
			{
				if (tcpClient.Connected)
				{
					return string.Format("正連線到 {0}...開啟到主機的連線， 在連接埠 {1}: 連線成功", domain, port);
				}

				logger.LogWarning("{logWarning}", string.Format("[PORT] {0}:{1} 等待連線{2}秒", domain, port, timeoutCount));

				timeoutCount++;

				await Task.Delay(
				delay: TimeSpan.FromSeconds(1),
					cancellationToken: HttpContext.RequestAborted)
					.ConfigureAwait(false);
			} while (timeoutCount < timeoutMaxCount && !tcpClient.Connected);

			// show result
			return string.Format("正連線到 {0}...無法開啟到主機的連線， 在連接埠 {1}: 逾時連線{2}秒", domain, port, timeoutMaxCount);
		}
		catch
		{
			return string.Format("正連線到 {0}...無法開啟到主機的連線， 在連接埠 {1}: 連線失敗", domain, port);
		}
	}
}
