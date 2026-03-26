using MediatR;
using System;

namespace Core.Application.Mediatr.Search.Commands
{
    public class DeleteSearchHistoryItemCommand : IRequest<Unit>
    {
        public string Id { get; set; } // Assuming SearchHistory entity has an int Id
    }
} 