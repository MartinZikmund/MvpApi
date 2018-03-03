﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MvpApi.Common.Models;
using MvpApi.Uwp.Views;
using Newtonsoft.Json;

// See https://github.com/LanceMcCarthy/MvpApi/issues/8 for details on the requirements of the model value entries
namespace MvpApi.Uwp.ViewModels
{
    public class ContributionDetailViewModel : PageViewModelBase
    {
        #region Fields

        private ContributionsModel originalContribution;
        private ContributionsModel selectedContribution;
        private bool isCurrentContributionDirty;
        private bool isContributionTypeEditable = true;
        private string urlHeader = "Url";
        private string annualQuantityHeader = "Annual Quantity";
        private string secondAnnualQuantityHeader = "Second Annual Quantity";
        private string annualReachHeader = "Annual Reach";
        private bool isUrlRequired;
        private bool isAnnualQuantityRequired;
        private bool isSecondAnnualQuantityRequired;
        private bool canSave;
        private bool canUpload = true;
        private bool useFastMode = true;
        private ObservableCollection<ContributionAreaContributionModel> categoryAreas;

        #endregion

        public ContributionDetailViewModel()
        {
            if (DesignMode.DesignModeEnabled || DesignMode.DesignMode2Enabled)
                LoadDesignTimeData();

            UploadQueue.CollectionChanged += UploadQueue_CollectionChanged;
        }

        private void UploadQueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CanUpload = UploadQueue.Any();
        }

        #region Properties

        public ObservableCollection<ContributionsModel> UploadQueue { get; set; } = new ObservableCollection<ContributionsModel>();

        public ContributionsModel SelectedContribution
        {
            get => selectedContribution;
            set => Set(ref selectedContribution, value);
        }

        public ObservableCollection<ContributionTypeModel> Types { get; set; } = new ObservableCollection<ContributionTypeModel>();
        
        public ObservableCollection<ContributionAreaContributionModel> CategoryAreas
        {
            get => categoryAreas;
            set => Set(ref categoryAreas, value);
        }
        
        public ObservableCollection<VisibilityViewModel> Visibilies { get; set; } = new ObservableCollection<VisibilityViewModel>();
        
        public bool IsCurrentContributionDirty
        {
            get => isCurrentContributionDirty;
            set => Set(ref isCurrentContributionDirty, value);
        }

        public bool IsContributionTypeEditable
        {
            get => isContributionTypeEditable;
            set => Set(ref isContributionTypeEditable, value);
        }

        public string AnnualQuantityHeader
        {
            get => annualQuantityHeader;
            set => Set(ref annualQuantityHeader, value);
        }

        public string SecondAnnualQuantityHeader
        {
            get => secondAnnualQuantityHeader;
            set => Set(ref secondAnnualQuantityHeader, value);
        }

        public string AnnualReachHeader
        {
            get => annualReachHeader;
            set => Set(ref annualReachHeader, value);
        }

        public string UrlHeader
        {
            get => urlHeader;
            set => Set(ref urlHeader, value);
        }

        public bool IsUrlRequired
        {
            get => isUrlRequired;
            set => Set(ref isUrlRequired, value);
        }

        public bool IsAnnualQuantityRequired
        {
            get => isAnnualQuantityRequired;
            set => Set(ref isAnnualQuantityRequired, value);
        }

        public bool IsSecondAnnualQuantityRequired
        {
            get => isSecondAnnualQuantityRequired;
            set => Set(ref isSecondAnnualQuantityRequired, value);
        }

        public bool CanSave
        {
            get => canSave;
            set => Set(ref canSave, value);
        }

        public bool CanUpload
        {
            get => canUpload;
            set => Set(ref canUpload, value);
        }

        public bool UseFastMode
        {
            get => useFastMode;
            set => Set(ref useFastMode, value);
        }

        #endregion

        #region Event handlers

        public void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            EvaluateCanSave();
        }

        public void UrlBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            EvaluateCanSave();
        }

        public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EvaluateCanSave();
        }

        public async void DatePicker_OnDateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            if (e.NewDate > new DateTime(2016, 10, 1) || e.NewDate <= new DateTime(2018, 3, 31))
            {
                return;
            }

            await new MessageDialog("The contribution date must be after the start of your current award period and before March 31, 2018 in order for it to count towards your evaluation", "Notice: Out of range").ShowAsync();
        }

        public void AnnualQuantityBox_OnValueChanged(object sender, EventArgs e)
        {
            EvaluateCanSave();
        }

        public void SecondAnnualQuantityBox_OnValueChanged(object sender, EventArgs e)
        {
            EvaluateCanSave();
        }

        public void ActivityType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHeaders(SelectedContribution.ContributionType);

            SelectedContribution.ContributionTypeName = SelectedContribution.ContributionType.EnglishName;

            EvaluateCanSave();
        }
        
        public async void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate all required fields
            var isValidated = await ValidateRequiredFields();

            if (!isValidated)
            {
                await new MessageDialog("You need to fill in every field marked as 'Required' before starting a new contribution.").ShowAsync();
                return;
            }

            SetupNextEntry();
        }

        public async void ClearQueueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var md = new MessageDialog("You are about to clear all of the contributions in the upload queue, are you sure?", "CLEAR");
                md.Commands.Add(new UICommand("YES"));
                md.Commands.Add(new UICommand("whoa, no!"));

                var dialogResult = await md.ShowAsync();

                if (dialogResult.Label != "YES")
                    return;

                UploadQueue.Clear();

                SetupNextEntry();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Something went wrong clearing the queue, please try again. Error: {ex.Message}").ShowAsync();
            }
        }

        public async void UploadContributionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var contribution in UploadQueue)
            {
                contribution.UploadStatus = UploadStatus.InProgress;

                var success = await UploadContributionAsync(contribution);

                // Mark success or failure
                contribution.UploadStatus = success ? UploadStatus.Success : UploadStatus.Failed;
            }
            
            var md = new MessageDialog("Would you like to clear the successful uploads from the queue?", "Complete");
            md.Commands.Add(new UICommand("yes"));
            md.Commands.Add(new UICommand("no"));

            var dialogResult = await md.ShowAsync();

            if (dialogResult.Label == "yes")
            {
                var successes = UploadQueue.Where(c => c.UploadStatus == UploadStatus.Success);

                foreach (var successfulUpload in successes)
                {
                    UploadQueue.Remove(successfulUpload);
                }
            }
            
            SelectedContribution = UploadQueue.LastOrDefault();
        }

        public async void DeleteContributionButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO Add Delete functionality back in once I add a flag for existing item
            await new MessageDialog("temporarily disabling contribution deletion", "Temporarily Disabled").ShowAsync();
            return;

            try
            {
                var md = new MessageDialog("Are you sure you want to delete this contribution?", "Delete Contribution?");
                md.Commands.Add(new UICommand("DELETE"));
                md.Commands.Add(new UICommand("cancel"));

                var dialogResult = await md.ShowAsync();

                if (dialogResult.Label != "DELETE")
                    return;

                var result = await App.ApiService.DeleteContributionAsync(SelectedContribution);

                //if (result == true)
                //{
                //    if(NavigationService.CanGoBack)
                //        NavigationService.GoBack();
                //}
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Something went wrong deleting this item, please try again. Error: {ex.Message}").ShowAsync();
            }
        }
        
        #endregion

        #region Methods

        // TODO Temporarily disabled - need to fix a few things before full validation is working
        private void EvaluateCanSave()
        {
            return;

            // These are all required, no matter the activity type
            if (string.IsNullOrEmpty(SelectedContribution?.Title) ||
                string.IsNullOrEmpty(SelectedContribution?.ContributionTypeName) ||
                SelectedContribution?.ContributionType == null ||
                SelectedContribution?.Visibility == null ||
                SelectedContribution?.ContributionTechnology == null)
            {
                CanSave = false;
            }
            else if (IsAnnualQuantityRequired && SelectedContribution?.AnnualQuantity == null)
            {
                CanSave = false;
            }
            else if (IsSecondAnnualQuantityRequired && SelectedContribution?.SecondAnnualQuantity == null)
            {
                CanSave = false;
            }
            else if (IsUrlRequired && string.IsNullOrEmpty(SelectedContribution?.ReferenceUrl))
            {
                CanSave = false;
            }
            else
            {
                CanSave = true;
            }
        }
        
        public async Task<bool> UploadContributionAsync(ContributionsModel contribution)
        {
            try
            {
                var submissionResult = await App.ApiService.SubmitContributionAsync(contribution);

                // copying back the ID which was created on the server once the item was added to the database
                contribution.ContributionId = submissionResult.ContributionId;
                
                return true;
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Something went wrong saving the item, please try again. Error: {ex.Message}").ShowAsync();
                return false;
            }
        }

        private void SetupNextEntry()
        {
            ContributionsModel nextItem;

            if (UseFastMode)
            {
                nextItem = new ContributionsModel
                {
                    StartDate = DateTime.Now,
                    Visibility = SelectedContribution?.Visibility,
                    ContributionType = SelectedContribution?.ContributionType,
                    ContributionTypeName = SelectedContribution?.ContributionType?.Name,
                    ContributionTechnology = SelectedContribution?.ContributionTechnology
                };
            }
            else
            {
                nextItem = new ContributionsModel
                {
                    StartDate = DateTime.Now,
                    Visibility = Visibilies.FirstOrDefault(),
                    ContributionType = Types.FirstOrDefault(),
                    ContributionTechnology = CategoryAreas?.FirstOrDefault()?.ContributionAreas?.FirstOrDefault()
                };
            }

            UploadQueue.Add(nextItem);

            SelectedContribution = UploadQueue.LastOrDefault();
        }
        
        private async Task<bool> ValidateRequiredFields()
        {
            if (SelectedContribution.ContributionType == null)
            {
                await new MessageDialog($"The Contribution Type is a required field").ShowAsync();
                return false;
            }

            if (SelectedContribution.Visibility == null)
            {
                await new MessageDialog($"The Visibility is a required field").ShowAsync();
                return false;
            }

            if (SelectedContribution.ContributionTechnology == null)
            {
                await new MessageDialog($"The Category Technology is a required field").ShowAsync();
                return false;
            }

            if (IsUrlRequired && string.IsNullOrEmpty(SelectedContribution.ReferenceUrl))
            {
                await new MessageDialog($"The {UrlHeader} field is required when entering a {SelectedContribution.ContributionType.EnglishName} Activity Type", 
                    $"Missing {UrlHeader}!").ShowAsync();

                return false;
            }

            if (IsAnnualQuantityRequired && SelectedContribution.AnnualQuantity == null)
            {
                await new MessageDialog($"The {AnnualQuantityHeader} field is required when entering a {SelectedContribution.ContributionType.EnglishName} Activity Type", 
                    $"Missing {AnnualQuantityHeader}!").ShowAsync();

                return false;
            }

            if (IsSecondAnnualQuantityRequired && SelectedContribution.SecondAnnualQuantity == null)
            {
                await new MessageDialog($"The {SecondAnnualQuantityHeader} field is required when entering a {SelectedContribution.ContributionType.EnglishName} Activity Type", 
                    $"Missing {SecondAnnualQuantityHeader}!").ShowAsync();

                return false;
            }

            return true;
        }

        private void UpdateHeaders(ContributionTypeModel contributionType)
        {
            switch (contributionType.EnglishName)
            {
                case "Article":
                    AnnualQuantityHeader = "Number of Articles";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Views";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Blog Site Posts":
                    AnnualQuantityHeader = "Number of Posts";
                    SecondAnnualQuantityHeader = "Number of Subscribers";
                    AnnualReachHeader = "Annual Unique Visitors";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Book (Author)":
                    AnnualQuantityHeader = "Number of Books";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Copies Sold";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Book (Co-Author)":
                    AnnualQuantityHeader = "Number of Books";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Copies Sold";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Code Project/Tools":
                    AnnualQuantityHeader = "Number of Projects";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Downloads";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Code Samples":
                    AnnualQuantityHeader = "Number of Samples";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Downloads";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Conference (booth presenter)":
                    AnnualQuantityHeader = "Number of Conferences";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Visitors";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Conference (organizer)":
                    AnnualQuantityHeader = "Number of Conferences";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Visitors";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Forum Moderator":
                    AnnualQuantityHeader = "Number of Threads Moderated";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Annual Reach";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Forum Participation (3rd Party Forums)":
                    AnnualQuantityHeader = "Number of Answers";
                    SecondAnnualQuantityHeader = "Number of Posts";
                    AnnualReachHeader = "Views of Answers";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = false;
                    IsSecondAnnualQuantityRequired = true;
                    break;
                case "Forum Participation (Microsoft Forums)":
                    AnnualQuantityHeader = "Number of Answers";
                    SecondAnnualQuantityHeader = "Number of Posts";
                    AnnualReachHeader = "Views of Answers";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Mentorship":
                    AnnualQuantityHeader = "Number of Mentees";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Annual Reach";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Open Source Project(s)":
                    AnnualQuantityHeader = "Project(s)";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Commits";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Other":
                    AnnualQuantityHeader = "Annual Quantity";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Annual Reach";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Product Group Feedback":
                    AnnualQuantityHeader = "Number of Events provided";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Feedbacks provided";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Site Owner":
                    AnnualQuantityHeader = "Posts";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Visitors";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Speaking (Conference)":
                    AnnualQuantityHeader = "Talks";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Attendees of talks";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Speaking (Local)":
                    AnnualQuantityHeader = "Talks";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Attendees of talks";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Speaking (User group)":
                    AnnualQuantityHeader = "Talks";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Attendees of talks";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Technical Social Media (Twitter, Facebook, LinkedIn...)":
                    AnnualQuantityHeader = "Number of Posts";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Followers";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Translation Review, Feedback and Editing":
                    AnnualQuantityHeader = "Annual Quantity";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Annual Reach";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "User Group Owner":
                    AnnualQuantityHeader = "Meetings";
                    SecondAnnualQuantityHeader = "Members";
                    AnnualReachHeader = "";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Video":
                    AnnualQuantityHeader = "Number of Videos";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Number of Views";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Webcast":
                    AnnualQuantityHeader = "Number of Videos";
                    SecondAnnualQuantityHeader = "Number of Views";
                    AnnualReachHeader = "";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                case "Website Posts":
                    AnnualQuantityHeader = "Number of Posts";
                    SecondAnnualQuantityHeader = "Number of Subscribers";
                    AnnualReachHeader = "Annual Unique Visitors";
                    IsUrlRequired = true;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
                default: // Fall back on 'other'
                    AnnualQuantityHeader = "Annual Quantity";
                    SecondAnnualQuantityHeader = "";
                    AnnualReachHeader = "Annual Reach";
                    IsUrlRequired = false;
                    IsAnnualQuantityRequired = true;
                    IsSecondAnnualQuantityRequired = false;
                    break;
            }
        }
        
        #endregion
        

        #region Navigation
        
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (App.ShellPage.DataContext is ShellPageViewModel shellVm && shellVm.IsLoggedIn)
            {
                try
                {
                    IsBusy = true;

                    // ** Get the associated lists from the API **

                    IsBusyMessage = "getting types...";

                    foreach (var type in await App.ApiService.GetContributionTypesAsync())
                    {
                        Types.Add(type);
                    }
                    
                    IsBusyMessage = "getting technologies...";

                    var areaRoots = await App.ApiService.GetContributionAreasAsync();

                    var flattenedAreaRoots = new List<ContributionAreaContributionModel>();

                    // Flatten out the result so that we only need one ComboBox with a CollectionViewSource
                    foreach (var areaRoot in areaRoots)
                    {
                        foreach (var area in areaRoot.Contributions)
                        {
                            flattenedAreaRoots.Add(area);
                        }
                    }

                    // This populates a CollectionViewSource in the XAML
                    CategoryAreas = new ObservableCollection<ContributionAreaContributionModel>(flattenedAreaRoots);
                    

                    IsBusyMessage = "getting visibility options...";

                    foreach (var visibility in await App.ApiService.GetVisibilitiesAsync())
                    {
                        Visibilies.Add(visibility);
                    }
                    
                    // ** Determine if we're editing an existing contribution or creating a new one ** //

                    if (parameter is ContributionsModel param)
                    {
                        UploadQueue.Add(param);

                        SelectedContribution = UploadQueue.FirstOrDefault();
                        
                        // You can't edit the ContributionType after it has been submitted
                        IsContributionTypeEditable = false;

                        // Deep cloning the object to serve as a clean original to compare against when editing and determine if the item is dirty or not.
                        var json = JsonConvert.SerializeObject(param);
                        originalContribution = JsonConvert.DeserializeObject<ContributionsModel>(json);
                    }
                    else
                    {
                        var cont = new ContributionsModel
                        {
                            StartDate = DateTime.Now,
                            Visibility = Visibilies.FirstOrDefault(),
                            ContributionType = Types.FirstOrDefault(), // Note this field is READONLY when it's an existing contribution
                            ContributionTypeName = SelectedContribution?.ContributionType?.Name
                        };

                        UploadQueue.Add(cont);
                        SelectedContribution = UploadQueue.FirstOrDefault();

                        // -- TESTING ONLY -- 
                        //SelectedContribution.Title = "Test Upload";
                        //SelectedContribution.Description = "This is a test contribution upload from the UWP application I'm building for the MVP community to help them submit their 2018 contributions.";
                        //SelectedContribution.ReferenceUrl = "lancemccarthy.com";
                        //SelectedContribution.AnnualQuantity = 0;
                        //SelectedContribution.SecondAnnualQuantity = 0;
                        //SelectedContribution.AnnualReach = 0;

                        //IsCurrentContributionDirty = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadDataAsync Exception {ex}");
                }
                finally
                {
                    IsBusyMessage = "";
                    IsBusy = false;
                }
            }
            else
            {
                await NavigationService.NavigateAsync(typeof(LoginPage));
            }
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            return base.OnNavigatedFromAsync(pageState, suspending);
        }

        #endregion

        #region DesignTime Data

        private void LoadDesignTimeData()
        {
            UploadQueue.Add(new ContributionsModel
            {
                StartDate = DateTime.Now,
                Title = "Awesome Thing One",
                Description = "This is a very cool thing that I'm adding in for a contribution because I need some design time data :D",
                UploadStatus = UploadStatus.Pending
            });

            UploadQueue.Add(new ContributionsModel
            {
                StartDate = DateTime.Now,
                Title = "Awesome Thing Two",
                Description = "This is a very cool thing that I'm adding in for a contribution because I need some design time data :D",
                UploadStatus = UploadStatus.InProgress,
                ContributionTechnology = new ContributionTechnologyModel {Name = "Tech Name", AwardCategory = "Tech Award Category", AwardName = "Tech Award Name"}
            });

            UploadQueue.Add(new ContributionsModel
            {
                StartDate = DateTime.Now,
                Title = "Awesome Thing Three",
                Description = "This is a very cool thing that I'm adding in for a contribution because I need some design time data :D",
                UploadStatus = UploadStatus.Failed,
                ContributionTechnology = new ContributionTechnologyModel {Name = "Tech Name", AwardCategory = "Tech Award Category", AwardName = "Tech Award Name"}
            });

            UploadQueue.Add(new ContributionsModel
            {
                StartDate = DateTime.Now,
                Title = "Awesome Thing Four",
                Description = "This is a very cool thing that I'm adding in for a contribution because I need some design time data :D",
                UploadStatus = UploadStatus.Success,
                ContributionTechnology = new ContributionTechnologyModel {Name = "Tech Name", AwardCategory = "Tech Award Category", AwardName = "Tech Award Name"}
            });
        }

        #endregion
    }
}
