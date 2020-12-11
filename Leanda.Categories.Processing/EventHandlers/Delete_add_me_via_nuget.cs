using MassTransit;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Commands
{
    public interface DeleteEntityCategories : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid EntityId { get; }
        List<Guid> CategoriesIds { get; }
        Guid UserId { get; }
    }
}
