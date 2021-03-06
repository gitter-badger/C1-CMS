﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composite.Data;
using Composite.Data.Types;
using Composite.C1Console.Elements;
using Composite.C1Console.Elements.Plugins.ElementProvider;
using Composite.Core.ResourceSystem;
using Composite.Core.ResourceSystem.Icons;
using Composite.C1Console.Security;
using Composite.C1Console.Workflow;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ObjectBuilder;
using Microsoft.Practices.ObjectBuilder;


namespace Composite.Plugins.Elements.ElementProviders.UserElementProvider
{
    internal sealed class UserElementProvider : IHooklessElementProvider, IAuxiliarySecurityAncestorProvider
    {
        private ElementProviderContext _elementProviderContext;

        public static ResourceHandle RootOpenIcon { get { return GetIconHandle("users-rootfolder-open"); } }
        public static ResourceHandle RootClosedIcon { get { return GetIconHandle("users-rootfolder-closed"); } }
        public static ResourceHandle GroupOpenIcon { get { return GetIconHandle("users-group-open"); } }
        public static ResourceHandle GroupClosedIcon { get { return GetIconHandle("users-group-closed"); } }
        public static ResourceHandle UserIcon { get { return GetIconHandle("users-user"); } }
        public static ResourceHandle LockedUserIcon { get { return GetIconHandle("users-user-disabled"); } }
        public static ResourceHandle AddUserIcon { get { return GetIconHandle("users-adduser"); } }
        public static ResourceHandle EditUserIcon { get { return GetIconHandle("users-edituser"); } }
        public static ResourceHandle DeleteUserIcon { get { return GetIconHandle("users-deleteuser"); } }

        private static readonly ActionGroup PrimaryActionGroup = new ActionGroup(ActionGroupPriority.PrimaryHigh);

        private static readonly PermissionType[] AddNewUserPermissionTypes = new PermissionType[] { PermissionType.Administrate };


        public UserElementProvider()
        {
            AuxiliarySecurityAncestorFacade.AddAuxiliaryAncestorProvider<DataEntityToken>(this);
        }



        public ElementProviderContext Context
        {
            set { _elementProviderContext = value; }
        }



        public IEnumerable<Element> GetRoots(SearchToken seachToken)
        {
            int userCount =
                (from user in DataFacade.GetData<IUser>()
                 select user).Count();

            Element element = new Element(_elementProviderContext.CreateElementHandle(new UserElementProviderEntityToken()))
            {
                VisualData = new ElementVisualizedData
                {
                    Label = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.RootLabel"),
                    ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.RootToolTip"),
                    HasChildren = userCount > 0,
                    Icon = UserElementProvider.RootClosedIcon,
                    OpenedIcon = UserElementProvider.RootOpenIcon
                }
            };

            element.AddAction(new ElementAction(new ActionHandle(new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Plugins.Elements.ElementProviders.UserElementProvider.AddNewUserWorkflow"), AddNewUserPermissionTypes)))
            {
                VisualData = new ActionVisualizedData
                {
                    Label = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.AddUserLabel"),
                    ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.AddUserToolTip"),
                    Icon = UserElementProvider.AddUserIcon,
                    Disabled = false,
                    ActionLocation = new ActionLocation
                    {
                        ActionType = ActionType.Add,
                        IsInFolder = false,
                        IsInToolbar = true,
                        ActionGroup = PrimaryActionGroup
                    }
                }
            });

            yield return element;
        }



        public IEnumerable<Element> GetChildren(EntityToken entityToken, SearchToken seachToken)
        {
            if (entityToken is UserElementProviderEntityToken)
            {
                return GetGroupChildrenElements(seachToken);
            }
            if (entityToken is UserElementProviderGroupEntityToken)
            {
                return GetUsersChildrenElements(entityToken.Id, seachToken);
            }
            return new Element[] { };
        }


        public Dictionary<EntityToken, IEnumerable<EntityToken>> GetParents(IEnumerable<EntityToken> entityTokens)
        {
            var result = new Dictionary<EntityToken, IEnumerable<EntityToken>>();

            foreach (EntityToken entityToken in entityTokens)
            {
                DataEntityToken dataEntityToken = entityToken as DataEntityToken;

                Type type = dataEntityToken.InterfaceType;
                if (type != typeof(IUser)) continue;

                IUser user = dataEntityToken.Data as IUser;

                var newEntityToken = new UserElementProviderGroupEntityToken(user.Group);

                result.Add(entityToken, new EntityToken[] { newEntityToken });
            }

            return result;
        }        



        private IEnumerable<Element> GetGroupChildrenElements(SearchToken seachToken)
        {
            IEnumerable<string> groups;

            if (seachToken.IsValidKeyword() == false)
            {
                groups =
                    (from user in DataFacade.GetData<IUser>()
                     orderby user.Group
                     select user.Group).Distinct().ToList();
            }
            else
            {
                string keyword = seachToken.Keyword.ToLowerInvariant();

                groups =
                    (from user in DataFacade.GetData<IUser>().ToList()
                     where user.Username.ToLowerInvariant().Contains(keyword)
                     orderby user.Group
                     select user.Group).Distinct().ToList();
            }

            List<Element> children = new List<Element>();

            foreach (string group in groups)
            {
                Element element = new Element(_elementProviderContext.CreateElementHandle(new UserElementProviderGroupEntityToken(group)))
                {
                    VisualData = new ElementVisualizedData
                    {
                        Label = group,
                        ToolTip = group,
                        HasChildren = true,
                        Icon = UserElementProvider.GroupClosedIcon,
                        OpenedIcon = UserElementProvider.GroupOpenIcon
                    }
                };

                element.AddAction(new ElementAction(new ActionHandle(new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Plugins.Elements.ElementProviders.UserElementProvider.AddNewUserWorkflow"), AddNewUserPermissionTypes)))
                {
                    VisualData = new ActionVisualizedData
                    {
                        Label = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.AddUserLabel"),
                        ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.AddUserToolTip"),
                        Icon = UserElementProvider.AddUserIcon,
                        Disabled = false,
                        ActionLocation = new ActionLocation
                        {
                            ActionType = ActionType.Add,
                            IsInFolder = false,
                            IsInToolbar = true,
                            ActionGroup = PrimaryActionGroup
                        }
                    }
                });

                children.Add(element);
            }

            return children;
        }



        private IEnumerable<Element> GetUsersChildrenElements(string groupName, SearchToken seachToken)
        {
            IEnumerable<IUser> users;

            if (seachToken.IsValidKeyword() == false)
            {
                users =
                    (from user in DataFacade.GetData<IUser>()
                     where user.Group == groupName
                     orderby user.Username
                     select user).ToList();
            }
            else
            {
                string keyword = seachToken.Keyword.ToLowerInvariant();

                users =
                    (from user in DataFacade.GetData<IUser>().ToList()
                     where user.Group == groupName &&
                           user.Username.ToLowerInvariant().Contains(keyword)
                     orderby user.Username
                     select user).ToList();
            }

            var children = new List<Element>();

            foreach (IUser user in users)
            {
                var element = new Element(_elementProviderContext.CreateElementHandle(user.GetDataEntityToken()))
                {
                    VisualData = new ElementVisualizedData
                    {
                        Label = user.GetLabel(),
                        ToolTip = user.GetLabel(),
                        HasChildren = false,
                        Icon = !user.IsLocked ? UserIcon : LockedUserIcon,
                    }
                };

                // Making "Edit permissions" not show up on user elements - it's confusing :)
                element.ElementExternalActionAdding = ElementExternalActionAddingExtensions.Remove(element.ElementExternalActionAdding, ElementExternalActionAdding.AllowManageUserPermissions);

                element.AddAction(new ElementAction(new ActionHandle(new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Plugins.Elements.ElementProviders.UserElementProvider.EditUserWorkflow"), new PermissionType[] { PermissionType.Administrate })))
                {
                    VisualData = new ActionVisualizedData
                    {
                        Label = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.EditUserLabel"),
                        ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.EditUserToolTip"),
                        Icon = UserElementProvider.EditUserIcon,
                        Disabled = false,
                        ActionLocation = new ActionLocation
                        {
                            ActionType = ActionType.Edit,
                            IsInFolder = false,
                            IsInToolbar = true,
                            ActionGroup = PrimaryActionGroup
                        }
                    }
                });

                element.AddAction(new ElementAction(new ActionHandle(new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Plugins.Elements.ElementProviders.UserElementProvider.DeleteUserWorkflow"), new PermissionType[] { PermissionType.Administrate })))
                {
                    VisualData = new ActionVisualizedData
                    {
                        Label = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.DeleteUserLabel"),
                        ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.DeleteUserToolTip"),
                        Icon = UserElementProvider.DeleteUserIcon,
                        Disabled = false,
                        ActionLocation = new ActionLocation
                        {
                            ActionType = ActionType.Delete,
                            IsInFolder = false,
                            IsInToolbar = true,
                            ActionGroup = PrimaryActionGroup
                        }
                    }
                });

                children.Add(element);
            }

            return children;
        }



        private static ResourceHandle GetIconHandle(string name)
        {
            return new ResourceHandle(BuildInIconProviderName.ProviderName, name);
        }
    }




    internal sealed class UserElementProviderAssembler : IAssembler<IHooklessElementProvider, HooklessElementProviderData>
    {
        public IHooklessElementProvider Assemble(IBuilderContext context, HooklessElementProviderData objectConfiguration, IConfigurationSource configurationSource, ConfigurationReflectionCache reflectionCache)
        {
            return new UserElementProvider();
        }
    }
}
