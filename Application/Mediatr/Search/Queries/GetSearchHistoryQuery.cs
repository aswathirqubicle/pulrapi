using MediatR;
using System.Collections.Generic;
using Application.DTOs.Search;

namespace Core.Application.Mediatr.Search.Queries
{
    // Renamed from GetSearchHistorySearchQuery
    public class GetSearchHistoryQuery : IRequest<List<SearchHistoryResponseDto>>
    {
    }
} 