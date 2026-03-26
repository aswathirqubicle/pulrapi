using Dashboard.Application.Models.AppLogs;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.Models;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Dashboard.Application.Hubs;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using System.Text.Json;
using System.Diagnostics;

namespace Dashboard.Application.Mediatr.AppLogs.Queries
{
    public class GetAppLogsQuery : PagingParamsRequest, IRequest<PagingResponse<AppLogResponse>>
    {
        public DateTime? LastLogTimestamp { get; set; }
    }

    public class GetAppLogsQueryHandler : IRequestHandler<GetAppLogsQuery, PagingResponse<AppLogResponse>>
    {
        private readonly ILogger<GetAppLogsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHubContext<AppLogsNotificationHub, INotificationHub> _appLogsHubContext;

        public GetAppLogsQueryHandler(ILogger<GetAppLogsQueryHandler> logger,
            IApplicationDbContext dbContext,
            IConfiguration configuration,
            IMapper mapper,
            IHubContext<AppLogsNotificationHub, INotificationHub> appLogsHubContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _mapper = mapper;
            _appLogsHubContext = appLogsHubContext;
        }

        public async Task<PagingResponse<AppLogResponse>> Handle(GetAppLogsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var listMapped = new PagingResponse<AppLogResponse>();

                var credentials = new BasicAWSCredentials(_configuration["Aws:CloudwatchAccessKey"], _configuration["Aws:CloudwatchSecret"]);
                using (var client = new AmazonCloudWatchLogsClient(credentials, RegionEndpoint.MESouth1))
                {
                    var describeLogStreamsRequest = new DescribeLogStreamsRequest()
                    {
                        LogGroupName = _configuration["Aws:CloudwatchLogGroup"],
                        Limit = 1,
                        OrderBy = OrderBy.LastEventTime,
                        Descending = true
                    };
                    var describeLogStreamsResult = await client.DescribeLogStreamsAsync(describeLogStreamsRequest, cancellationToken);

                    if (describeLogStreamsResult.LogStreams.Count == 0)
                    {
                        return listMapped;
                    }

                    var stream = describeLogStreamsResult.LogStreams[0];
                    var eventsRequest = new GetLogEventsRequest()
                    {
                        LogStreamName = stream.LogStreamName,
                        LogGroupName = describeLogStreamsRequest.LogGroupName,
                        Limit = request.PageSize
                    };

                    var firstEventTimestamp = stream.FirstEventTimestamp.HasValue
                        ? new DateTimeOffset(stream.FirstEventTimestamp.Value).ToUnixTimeMilliseconds()
                        : 0L;
                    var lastEventTimestamp = stream.LastEventTimestamp.HasValue
                        ? new DateTimeOffset(stream.LastEventTimestamp.Value).ToUnixTimeMilliseconds()
                        : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var queryNumberOfEvents = await client.StartQueryAsync(new StartQueryRequest()
                    {
                        LogGroupName = _configuration["Aws:CloudwatchLogGroup"],
                        QueryString = $"fields @timestamp | filter tomillis(@timestamp) >= {firstEventTimestamp} | stats count() as @count",
                        StartTime = firstEventTimestamp,
                        EndTime = lastEventTimestamp,
                    });

                    int? totalLogsCount = null;
                    GetQueryResultsResponse? numberOfEventsQueryResult = null;
                    bool weHaveResult = false;

                    var timer = Stopwatch.StartNew();
                    while (!weHaveResult)
                    {
                        numberOfEventsQueryResult = await client.GetQueryResultsAsync(new GetQueryResultsRequest { QueryId = queryNumberOfEvents.QueryId });
                        if ((numberOfEventsQueryResult.Status != QueryStatus.Running && numberOfEventsQueryResult.Status != QueryStatus.Scheduled) ||
                            timer.ElapsedMilliseconds >= 5000)
                        {
                            weHaveResult = true;
                            timer.Stop();
                        }
                    }

                    if (numberOfEventsQueryResult?.Results?.Count > 0)
                    {
                        var countValue = numberOfEventsQueryResult.Results[0].Find(e => e.Field == "@count")?.Value;
                        totalLogsCount = countValue != null ? int.Parse(countValue) : null;
                    }

                    if (request.LastLogTimestamp.HasValue)
                    {
                        var ticks = request.LastLogTimestamp.Value.Ticks;
                        var milliseconds = ticks / TimeSpan.TicksPerMillisecond;
                        eventsRequest.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime.AddMilliseconds(1);
                    }

                    var result = await client.GetLogEventsAsync(eventsRequest);
                    var resultList = new List<AppLogResponse>();

                    foreach (var outputLogEvent in result.Events)
                    {
                        var appLogRes = JsonSerializer.Deserialize<AppLogResponse>(outputLogEvent.Message)!;
                        resultList.Add(appLogRes);
                    }

                    var pagedList = PagedList<AppLogResponse>.ToPagedList(resultList, request.PageNumber, request.PageSize, totalLogsCount);
                    listMapped = _mapper.Map<PagingResponse<AppLogResponse>>(pagedList);
                }

                return listMapped;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
