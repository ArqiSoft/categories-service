﻿using MassTransit;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Commands
{
    public interface DeleteCategoriesFromEntity : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        List<Guid> CategoriesIds { get; set; }
        Guid UserId { get; }
    }
}
