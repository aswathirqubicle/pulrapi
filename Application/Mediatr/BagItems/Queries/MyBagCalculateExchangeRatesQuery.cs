using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.BagItems;

namespace Core.Application.Mediatr.BagItems.Queries
{
    public class CalculateExchangeRatesBagItemsCommand : IRequest<BagItemsExchangeRatesResponse>
    {
        public string MyCurrencyCode { get; set; }
        public List<BagProductExchangeRateDto> Products { get; set; }
    }

    public class CalculateExchangeRatesBagItemsCommandHandler : IRequestHandler<CalculateExchangeRatesBagItemsCommand, BagItemsExchangeRatesResponse>
    {
        private readonly ILogger<CalculateExchangeRatesBagItemsCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IExchangeRateService _exchangeRateService;

        public CalculateExchangeRatesBagItemsCommandHandler(ILogger<CalculateExchangeRatesBagItemsCommandHandler> logger,
            IMapper mapper,
            IExchangeRateService exchangeRateService)
        {
            _logger = logger;
            _mapper = mapper;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<BagItemsExchangeRatesResponse> Handle(CalculateExchangeRatesBagItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var bagItemsExchangeRatesResponse = new BagItemsExchangeRatesResponse();
                var currencyCodes = new List<string>() { request.MyCurrencyCode };
                currencyCodes.AddRange(request.Products.Select(p => p.CurrencyCode).Distinct().ToList());

                var exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);

                foreach (var item in request.Products)
                {
                    item.Price = _exchangeRateService.GetCurrencyExchangeRates(item.CurrencyCode, request.MyCurrencyCode, (double)item.Price, exchangeRates);
                    item.CurrencyCode = request.MyCurrencyCode;
                    bagItemsExchangeRatesResponse.Products.Add(item);
                }

                return bagItemsExchangeRatesResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
