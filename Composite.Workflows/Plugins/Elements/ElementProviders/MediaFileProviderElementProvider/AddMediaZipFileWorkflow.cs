﻿using System;
using System.Linq;
using System.Workflow.Activities;
using Composite.C1Console.Actions;
using Composite.C1Console.Events;
using Composite.C1Console.Forms.CoreUiControls;
using Composite.C1Console.Workflow;
using Composite.C1Console.Workflow.Activities;
using Composite.Data;
using Composite.Data.Types;
using ICSharpCode.SharpZipLib.Zip;


namespace Composite.Plugins.Elements.ElementProviders.MediaFileProviderElementProvider
{
    [AllowPersistingWorkflow(WorkflowPersistingType.Never)]
    public sealed partial class AddMediaZipFileWorkflow : Composite.C1Console.Workflow.Activities.FormsWorkflow
    {
        [NonSerialized]
        bool _zipHasBeenUploaded = false;

        public AddMediaZipFileWorkflow()
        {
            InitializeComponent();
        }



        private void InitializeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            FormsWorkflow workflow = this.GetRoot<FormsWorkflow>();

            string parentFolderPath;
            string storeId;
            if (this.EntityToken is MediaRootFolderProviderEntityToken)
            {
                MediaRootFolderProviderEntityToken token = (MediaRootFolderProviderEntityToken)this.EntityToken;
                parentFolderPath = "/";
                storeId = token.Id;
            }
            else
            {
                DataEntityToken token = (DataEntityToken)this.EntityToken;
                IMediaFileFolder parentFolder = (IMediaFileFolder)token.Data;
                parentFolderPath = parentFolder.Path;
                storeId = parentFolder.StoreId;
            }

            string providerName = DataFacade.GetData<IMediaFileStore>().Where(x => x.Id == storeId).First().DataSourceId.ProviderName;

            UploadedFile file = new UploadedFile();

            workflow.Bindings.Add("UploadedFile", file);
            workflow.Bindings.Add("RecreateFolders", true);
            workflow.Bindings.Add("OverwriteExisting", false);
            workflow.Bindings.Add("ParentFolderPath", parentFolderPath);
            workflow.Bindings.Add("ProviderName", providerName);
        }



        private void HandleFinish_ExecuteCode(object sender, EventArgs e)
        {
            UploadedFile uploadedFile = this.GetBinding<UploadedFile>("UploadedFile");
            bool recreateFolders = this.GetBinding<bool>("RecreateFolders");
            bool overwrite = this.GetBinding<bool>("OverwriteExisting");
            string parentFolderPath = this.GetBinding<string>("ParentFolderPath");
            string providerName = this.GetBinding<string>("ProviderName");

            if (uploadedFile.HasFile)
            {
                if(uploadedFile.FileName.EndsWith(".docx"))
                {
                    ShowUploadError("${Composite.Management, Website.Forms.Administrative.AddZipMediaFile.CannotUploadDocxFile}");
                    return;
                }

                using (System.IO.Stream readStream = uploadedFile.FileStream)
                {
                    try
                    {
                        ZipMediaFileExtractor.AddZip(providerName, parentFolderPath, readStream, recreateFolders, overwrite);
                        _zipHasBeenUploaded = true;
                    }
                    catch (ZipException)
                    {
                    }
                }
            }

            if (!_zipHasBeenUploaded)
            {
                ShowUploadError("${Composite.Management, Website.Forms.Administrative.AddZipMediaFile.WrongUploadedFile.Message}");
            }
        }



        private void ZipWasUploaded(object sender, ConditionalEventArgs e)
        {
            e.Result = _zipHasBeenUploaded;
        }



        private void FinalizeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            AddNewTreeRefresher addNewTreeRefresher = this.CreateAddNewTreeRefresher(this.EntityToken);


            addNewTreeRefresher.PostRefreshMesseges(this.EntityToken);
        }



        private void HasUserUploaded(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            UploadedFile uploadedFile = this.GetBinding<UploadedFile>("UploadedFile");

            if (uploadedFile.HasFile == false)
            {
                ShowUploadError("${Composite.Management, Website.Forms.Administrative.AddZipMediaFile.MissingUploadedFile.Message}");
                e.Result = false;
                return;
            }

            e.Result = true;
        }

        private void ShowUploadError(string message)
        {
            //TODO: Cannot show an error bubble on file selector since the control doesn't support it. Should be fixed on client js 
            //this.ShowFieldMessage("UploadedFile", "${Composite.Management, Website.Forms.Administrative.AddZipMediaFile.MissingUploadedFile.Message}");
            this.ShowMessage(DialogType.Error, 
                "${Composite.Management, Website.Forms.Administrative.AddZipMediaFile.Error.Title}",
                message);
        }
    }
}
