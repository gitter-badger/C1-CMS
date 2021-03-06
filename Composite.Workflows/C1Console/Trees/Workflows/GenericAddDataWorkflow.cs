using System;
using System.Collections.Generic;
using System.Linq;
using System.Workflow.Runtime;
using System.Xml.Linq;
using Composite.C1Console.Actions;
using Composite.C1Console.Elements;
using Composite.C1Console.Workflow;
using Composite.Core.Serialization;
using Composite.Core.Types;
using Composite.Data;
using Composite.Data.DynamicTypes;
using Composite.Data.GeneratedTypes;
using Composite.Data.ProcessControlled;
using Composite.Data.ProcessControlled.ProcessControllers.GenericPublishProcessController;
using Composite.Data.Types;
using Composite.C1Console.Security;
using Composite.C1Console.Workflow.Foundation;



namespace Composite.C1Console.Trees.Workflows
{
    [AllowPersistingWorkflow(WorkflowPersistingType.Idle)]
    public sealed partial class GenericAddDataWorkflow : Composite.C1Console.Workflow.Activities.FormsWorkflow
    {
        [NonSerialized]
        private bool _doPublish = false;

        [NonSerialized]
        private IDictionary<string, string> _dataPayload = null;

        [NonSerialized]
        private DataTypeDescriptorFormsHelper _dataTypeDescriptorFormsHelper = null;

        [NonSerialized]
        private string _typeName = null;
        private string TypeName { get { return _typeName; } set { _typeName = value; } }

        private Type InterfaceType
        {
            get { return StringConversionServices.DeserializeValueType(DataPayload["_InterfaceType_"]); }
        }

        private string CustomFormMarkupPath
        {
            get
            {
                if (DataPayload.ContainsKey("_CustomFormMarkupPath_"))
                {
                    return StringConversionServices.DeserializeValueString(DataPayload["_CustomFormMarkupPath_"]);
                }

                return null;
            }
        }

        private string IconResourceName
        {
            get { return StringConversionServices.DeserializeValueString(DataPayload["_IconResourceName_"]); }
        }

        private void Initialize()
        {
            if (_dataTypeDescriptorFormsHelper == null)
            {
                if (String.IsNullOrEmpty(this.Payload))
                {
                    throw new InvalidOperationException("The interface type should be a part of the workflows payload");
                }

                Dictionary<string, string> serializedValues = StringConversionServices.ParseKeyValueCollection(this.Payload);

                _dataPayload = serializedValues;
            }

            if (!PermissionsFacade.GetPermissionsForCurrentUser(EntityToken).Contains(PermissionType.Publish) || !typeof(IPublishControlled).IsAssignableFrom(InterfaceType))
            {
                FormData formData = WorkflowFacade.GetFormData(InstanceId, true);

                if (formData.ExcludedEvents == null)
                    formData.ExcludedEvents = new List<string>();

                formData.ExcludedEvents.Add("SaveAndPublish");
            }
        }



        private IDictionary<string, string> DataPayload
        {
            get { return _dataPayload; }
        }



        private DataTypeDescriptorFormsHelper FormsHelper
        {
            get
            {
                if (_dataTypeDescriptorFormsHelper == null)
                {
                    DataTypeDescriptor dataTypeDescriptor = DynamicTypeManager.GetDataTypeDescriptor(InterfaceType);

                    this.TypeName = dataTypeDescriptor.Name;

                    GeneratedTypesHelper generatedTypesHelper = new GeneratedTypesHelper(dataTypeDescriptor) { AllowForiegnKeyEditing = true };

                    _dataTypeDescriptorFormsHelper = new DataTypeDescriptorFormsHelper(dataTypeDescriptor, true, this.EntityToken);
                    if (!string.IsNullOrEmpty(CustomFormMarkupPath))
                    {
                        _dataTypeDescriptorFormsHelper.CustomFormDefinition = 
                            XDocument.Load(CustomFormMarkupPath, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                    }
                    _dataTypeDescriptorFormsHelper.LayoutIconHandle = IconResourceName;
                    _dataTypeDescriptorFormsHelper.AddReadOnlyFields(generatedTypesHelper.NotEditableDataFieldDescriptorNames);
                }

                return _dataTypeDescriptorFormsHelper;
            }
        }



        public GenericAddDataWorkflow()
        {
            InitializeComponent();
        }



        private void initializeCodeActivity_BuildNewData_ExecuteCode(object sender, EventArgs e)
        {
            Initialize();

            this.FormsHelper.UpdateWithNewBindings(this.Bindings);

            IData newData = DataFacade.BuildNew(InterfaceType);

            if (PageFolderFacade.GetAllFolderTypes().Contains(InterfaceType))
            {
                Dictionary<string, string> piggybag = PiggybagSerializer.Deserialize(this.ExtraPayload);
                PiggybagDataFinder piggybagDataFinder = new PiggybagDataFinder(piggybag, this.EntityToken);
                IPage page = (IPage)piggybagDataFinder.TryGetData(typeof(IPage));
                if (page != null)
                {
                    PageFolderFacade.AssignFolderDataSpecificValues(newData, page);
                }
            }

            IPublishControlled publishControlled = newData as IPublishControlled;
            if (publishControlled != null)
            {
                publishControlled.PublicationStatus = GenericPublishProcessController.Draft;
            }


            Dictionary<string, string> values = new Dictionary<string, string>();
            TreeDataFieldGroupingElementEntityToken castedEntityToken = this.EntityToken as TreeDataFieldGroupingElementEntityToken;
            if (castedEntityToken != null)
            {
                Tree tree = TreeFacade.GetTree(castedEntityToken.Source);
                DataFolderElementsTreeNode treeNode = (DataFolderElementsTreeNode)tree.GetTreeNode(castedEntityToken.TreeNodeId);

                if ((treeNode.Range == null) && (treeNode.FirstLetterOnly == false))
                {
                    foreach (var kvp in castedEntityToken.DeserializedGroupingValues)
                    {
                        values.Add(kvp.Key, ValueTypeConverter.Convert<string>(kvp.Value));
                    }
                }
            }

            var props = InterfaceType.GetPropertiesRecursively().ToDictionary(prop => prop.Name);

            foreach (var kvp in this.DataPayload)
            {
                // Filtering payload data which is not default field values
                if (props.ContainsKey(kvp.Key))
                {
                    if (values.ContainsKey(kvp.Key) == false)
                    {
                        values.Add(kvp.Key, StringConversionServices.DeserializeValueString(kvp.Value));
                    }
                    else
                    {
                        values[kvp.Key] = StringConversionServices.DeserializeValueString(kvp.Value);
                    }
                }
            }

            newData.SetValues(values);



            this.FormsHelper.ObjectToBindings(newData, this.Bindings);

            GeneratedTypesHelper.SetNewIdFieldValue(newData);

            this.Bindings.Add("NewData", newData);
        }



        private void step1CodeActivity_DisplayForm_ExecuteCode(object sender, EventArgs e)
        {
            Initialize();

            if (this.BindingExist("DataAdded") == false)
            {
                this.FormsHelper.LayoutLabel = this.FormsHelper.DataTypeDescriptor.Name;
            }

            IData newData = this.GetBinding<IData>("NewData");

            this.DeliverFormData(
                    this.TypeName,
                    StandardUiContainerTypes.Document,
                    this.FormsHelper.GetForm(),
                    this.Bindings,
                    this.FormsHelper.GetBindingsValidationRules(newData)
                );
        }



        private void saveCodeActivity_SaveData_ExecuteCode(object sender, EventArgs e)
        {
            Initialize();

            bool isValid = ValidateBindings();

            IData newData = this.GetBinding<IData>("NewData");

            if (!BindAndValidate(FormsHelper, newData))
            {
                isValid = false;
            }

            if (isValid == false)
            {
                SetSaveStatus(false);
                return;
            }

            this.RefreshCurrentEntityToken();

            if (this.BindingExist("DataAdded") == false)
            {
                newData = DataFacade.AddNew(newData);

                this.AcquireLock(newData.GetDataEntityToken());

                this.UpdateBinding("NewData", newData);
                this.Bindings.Add("DataAdded", true);

                SetSaveStatus(true, newData);
            }
            else
            {
                DataFacade.Update(newData);
                SetSaveStatus(true);
            }

            PublishIfNeeded(newData);
        }



        private void PublishIfNeeded(IData newData)
        {
            if (newData is IPublishControlled && _doPublish)
            {
                GenericPublishProcessController.PublishActionToken actionToken = new GenericPublishProcessController.PublishActionToken();
                FlowControllerServicesContainer serviceContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                ActionExecutorFacade.Execute(newData.GetDataEntityToken(), actionToken, serviceContainer);
            }
        }


        private void enablePublishCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            _doPublish = true;
        }
    }
}