// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using System;
using Newtonsoft.Json;

namespace MvpApi.Common.Models
{
    public partial class ContributionsModel : ObservableObject
    {
        private string contributionTypeName;
        private ContributionTypeModel contributionType;
        private ContributionTechnologyModel contributionTechnology;
        private DateTime? startDate;
        private string title;
        private string referenceUrl;
        private VisibilityViewModel visibility;
        private int? annualQuantity;
        private int? secondAnnualQuantity;
        private int? annualReach;
        private string description;
        private int? contributionId;
        private UploadStatus uploadStatus;

        /// <summary>
        /// Initializes a new instance of the ContributionsModel class.
        /// </summary>
        public ContributionsModel() { }

        /// <summary>
        /// Initializes a new instance of the ContributionsModel class.
        /// </summary>
        public ContributionsModel(int? contributionId = default(int?), string contributionTypeName = default(string), ContributionTypeModel contributionType = default(ContributionTypeModel), ContributionTechnologyModel contributionTechnology = default(ContributionTechnologyModel), DateTime? startDate = default(DateTime?), string title = default(string), string referenceUrl = default(string), VisibilityViewModel visibility = default(VisibilityViewModel), int? annualQuantity = default(int?), int? secondAnnualQuantity = default(int?), int? annualReach = default(int?), string description = default(string))
        {
            ContributionId = contributionId;
            ContributionTypeName = contributionTypeName;
            ContributionType = contributionType;
            ContributionTechnology = contributionTechnology;
            StartDate = startDate;
            Title = title;
            ReferenceUrl = referenceUrl;
            Visibility = visibility;
            AnnualQuantity = annualQuantity;
            SecondAnnualQuantity = secondAnnualQuantity;
            AnnualReach = annualReach;
            Description = description;
        }

        /// <summary>
        /// Gets or sets the Contribution table id.
        /// </summary>
        [JsonProperty(PropertyName = "ContributionId")]
        public int? ContributionId
        {
            get => contributionId;
            set => SetProperty(ref contributionId, value);
        }

        /// <summary>
        /// Name of the contribution type
        /// </summary>
        [JsonProperty(PropertyName = "ContributionTypeName")]
        public string ContributionTypeName
        {
            get => contributionTypeName;
            set => SetProperty(ref contributionTypeName, value);
        }

        /// <summary>
        /// Gets or sets the contribution type.
        /// </summary>
        [JsonProperty(PropertyName = "ContributionType")]
        public ContributionTypeModel ContributionType
        {
            get => contributionType;
            set => SetProperty(ref contributionType, value);
        }

        /// <summary>
        /// Gets or sets the contribution technology.
        /// </summary>
        [JsonProperty(PropertyName = "ContributionTechnology")]
        public ContributionTechnologyModel ContributionTechnology
        {
            get => contributionTechnology;
            set => SetProperty(ref contributionTechnology, value);
        }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        [JsonProperty(PropertyName = "StartDate")]
        public DateTime? StartDate
        {
            get => startDate;
            set => SetProperty(ref startDate, value);
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonProperty(PropertyName = "Title")]
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        [JsonProperty(PropertyName = "ReferenceUrl")]
        public string ReferenceUrl
        {
            get => referenceUrl;
            set => SetProperty(ref referenceUrl, value);
        }

        /// <summary>
        /// Gets or sets the visibility.
        /// </summary>
        [JsonProperty(PropertyName = "Visibility")]
        public VisibilityViewModel Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        /// <summary>
        /// Gets or sets the annual quantity.
        /// </summary>
        [JsonProperty(PropertyName = "AnnualQuantity")]
        public int? AnnualQuantity
        {
            get => annualQuantity;
            set => SetProperty(ref annualQuantity, value);
        }

        /// <summary>
        /// Gets or sets the second annual quantity.
        /// </summary>
        [JsonProperty(PropertyName = "SecondAnnualQuantity")]
        public int? SecondAnnualQuantity
        {
            get => secondAnnualQuantity;
            set => SetProperty(ref secondAnnualQuantity, value);
        }

        /// <summary>
        /// Gets or sets the reach score.
        /// </summary>
        [JsonProperty(PropertyName = "AnnualReach")]
        public int? AnnualReach
        {
            get => annualReach;
            set => SetProperty(ref annualReach, value);
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonProperty(PropertyName = "Description")]
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }


        /// <summary>
        /// NOT PART OF THE MVP API Schema. This determines the upload status of a contribution contribution's upload success or failure. 
        /// </summary>
        [JsonIgnore]
        public UploadStatus UploadStatus
        {
            get => uploadStatus;
            set => SetProperty(ref uploadStatus, value);
        }
    }
}
