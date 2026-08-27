using System;

namespace SnowMeltingCalculator.Services.Project
{
    public readonly record struct ProjectSaveDates(DateTime PriorCreatedDate, DateTime Now)
    {
        public DateTime CreatedDate =>
            PriorCreatedDate == DateTime.MinValue ? Now : PriorCreatedDate;

        public DateTime ModifiedDate => Now;
    }
}
