using Core.Application.Security.Validation.Attributes;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Messages.Commands
{
    public class ReplyToStoryCommand : IRequest<Unit>
    {
        [SafeUid(allowNullValue: false, maxLength: 500, minLength: 1, validateGuidFormat: true)]
        public string StoryUid { get; set; }
        
        [SafeName(allowNullValue: false, maxLength: 500, minLength: 1)]
        public string Message { get; set; }
    }

    public class ReplyToStoryCommandHandler : IRequestHandler<ReplyToStoryCommand, Unit>
    {
        public async Task<Unit> Handle(ReplyToStoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // TODO
                await Task.Delay(1000, cancellationToken);
                return Unit.Value;
            }
            catch (Exception e)
            {

                throw new Exception($"Error replying to story: {e.Message}", e);
            }
        }
    }
}
