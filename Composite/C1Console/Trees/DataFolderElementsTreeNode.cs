﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Composite.Core.Extensions;
using Composite.Data;
using Composite.Data.ProcessControlled;
using Composite.C1Console.Elements;
using Composite.Core.Linq;
using Composite.Core.ResourceSystem;
using Composite.C1Console.Security;
using Composite.C1Console.Trees.Foundation;
using Composite.C1Console.Trees.Foundation.FolderRanges;
using Composite.Core.Types;
using Composite.C1Console.Users;


namespace Composite.C1Console.Trees
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    public class DataFolderElementsTreeNode : DataFilteringTreeNode
    {
        private readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethods().First(f => f.Name == "StartsWith");
        private readonly MethodInfo StringSubstringMethodInfo = typeof(string).GetMethods().Where(f => f.Name == "Substring").Skip(1).First();
        private readonly MethodInfo ToUpperCompareMethodInfo = typeof(string).GetMethods().First(f => f.Name == "ToUpper");

        /// <exclude />
        public Type InterfaceType { get; internal set; }            // Requried

        /// <exclude />
        public ResourceHandle Icon { get; internal set; }           // Defaults to 'folder-disabled'

        /// <exclude />
        public string FieldName { get; internal set; }              // Requried

        /// <exclude />
        public string DateFormat { get; internal set; }             // Optional

        /// <exclude />
        public string Range { get; internal set; }                  // Optional

        /// <exclude />
        public bool FirstLetterOnly { get; internal set; }          // Optional

        /// <exclude />
        public SortDirection SortDirection { get; internal set; }       // Optional

        /// <exclude />
        public LeafDisplayMode Display { get; internal set; }       // Optional

        /// <exclude />
        public bool ShowForeignItems { get; internal set; }         // Optional


        // Cached values        
        private PropertyInfo KeyPropertyInfo { get; set; }
        private string GroupingValuesFieldName { get; set; }
        private string SerializedInterfaceType { get; set; }
        private PropertyInfo PropertyInfo { get; set; }
        private DateTimeFormater DateTimeFormater { get; set; }
        private IFolderRanges FolderRanges { get; set; }
        private List<DataFolderElementsTreeNode> AllGroupingNodes { get; set; }
        private ConstructorInfo AllGroupingsTupleConstructor { get; set; }
        private MethodInfo FolderIndexListAddMethodInfo { get; set; }
        private MethodInfo FolderIndexListClearMethodInfo { get; set; }

        private object FolderIndexListObject { get; set; }
        private IQueryable FolderIndexListQueryable { get; set; }


        /// <summary>
        /// <value>True</value> if parent element is not a <see cref="DataFolderElementsTreeNode"/>
        /// </summary>
        private bool IsTopFolderParent { get; set; }
        private DataElementsTreeNode ChildGeneratingDataElementsTreeNode { get; set; }
        private ParentIdFilterNode ChildGeneratingParentIdFilterNode { get; set; }



        /// <exclude />
        public override IEnumerable<EntityToken> GetEntityTokens(EntityToken childEntityToken, TreeNodeDynamicContext dynamicContext)
        {
            IEnumerable<object> labels;

            if (!this.LocalizationEndabled)
            {
                labels = GetObjects(dynamicContext, true);
            }
            else
            {
                List<object> orgLabels = GetObjects(dynamicContext, true);
                using (new DataScope(UserSettings.ForeignLocaleCultureInfo))
                {
                    List<object> foriegnLabels = GetObjects(dynamicContext, true);
                    orgLabels.AddRange(foriegnLabels);
                    labels = orgLabels.Distinct();
                }
            }


            Type childGeneratingDataElementsReferenceType = null;
            object childGeneratingDataElementsReferenceValue = null;

            DataEntityToken dataEntityToken = childEntityToken as DataEntityToken;
            var treeDataFieldGroupingElementEntityToken = childEntityToken as TreeDataFieldGroupingElementEntityToken;
            if (dataEntityToken != null)
            {
                if (this.ChildGeneratingParentIdFilterNode != null)
                {
                    childGeneratingDataElementsReferenceType = this.ChildGeneratingParentIdFilterNode.ParentFilterType;
                    childGeneratingDataElementsReferenceValue = this.ChildGeneratingParentIdFilterNode.ReferencePropertyInfo.GetValue(dataEntityToken.Data, null);
                }
            }
            else if (treeDataFieldGroupingElementEntityToken != null)
            {
                childGeneratingDataElementsReferenceType = treeDataFieldGroupingElementEntityToken.ChildGeneratingDataElementsReferenceType;
                childGeneratingDataElementsReferenceValue = treeDataFieldGroupingElementEntityToken.ChildGeneratingDataElementsReferenceValue;
            }


            foreach (object label in labels)
            {
                var entityToken = new TreeDataFieldGroupingElementEntityToken(this.Id, this.Tree.TreeId, TypeManager.SerializeType(this.InterfaceType));

                TupleIndexer tupleIndexer = new TupleIndexer(label);

                entityToken.GroupingValues = new Dictionary<string, object>();

                int index = 1;
                foreach (DataFolderElementsTreeNode dataFolderElementsTreeNode in this.AllGroupingNodes)
                {
                    if (dataFolderElementsTreeNode.FolderRanges == null)
                    {
                        object fieldValue = tupleIndexer.GetAtIndex(index++);
                        fieldValue = ConvertFieldValue(dataFolderElementsTreeNode, fieldValue);
                        entityToken.GroupingValues.Add(dataFolderElementsTreeNode.GroupingValuesFieldName, fieldValue);
                    }
                    else
                    {
                        int fieldValue = (int)tupleIndexer.GetAtIndex(index++);
                        entityToken.FolderRangeValues.Add(dataFolderElementsTreeNode.FieldName, fieldValue);
                    }
                }

                entityToken.ChildGeneratingDataElementsReferenceType = childGeneratingDataElementsReferenceType;
                entityToken.ChildGeneratingDataElementsReferenceValue = childGeneratingDataElementsReferenceValue;

                yield return entityToken;
            }
        }



        internal override Type CurrentDataInterfaceType
        {
            get { return this.InterfaceType; }
        }



        /// <exclude />
        public override AncestorResult GetParentEntityToken(EntityToken childEntityToken, Type parentInterfaceOfInterest, TreeNodeDynamicContext dynamicContext)
        {
            if (this.ParentNode is DataFolderElementsTreeNode)
            {
                TreeDataFieldGroupingElementEntityToken childGroupingElementEntityToken = childEntityToken as TreeDataFieldGroupingElementEntityToken;

                if (childGroupingElementEntityToken != null)
                {
                    TreeDataFieldGroupingElementEntityToken newGroupingElementEntityToken = new TreeDataFieldGroupingElementEntityToken(
                        this.ParentNode.Id,
                        this.Tree.TreeId,
                        this.SerializedInterfaceType);

                    newGroupingElementEntityToken.GroupingValues = new Dictionary<string, object>(childGroupingElementEntityToken.GroupingValues);
                    newGroupingElementEntityToken.FolderRangeValues = new Dictionary<string, int>(childGroupingElementEntityToken.FolderRangeValues);

                    newGroupingElementEntityToken.GroupingValues.Remove(this.GroupingValuesFieldName);

                    return new AncestorResult(this.ParentNode, newGroupingElementEntityToken);
                }
            }            

            throw new NotImplementedException();
        }



        /// <exclude />
        protected override IEnumerable<Element> OnGetElements(EntityToken parentEntityToken, TreeNodeDynamicContext dynamicContext)
        {
            Type referenceType = null;
            object referenceValue = null;

            if (this.ChildGeneratingParentIdFilterNode != null)
            {
                if (this.IsTopFolderParent)
                {
                    DataEntityToken dataEntityToken = (DataEntityToken)dynamicContext.Piggybag
                        .GetParentEntityTokens(parentEntityToken)
                        .FirstOrDefault(f => f is DataEntityToken && ((DataEntityToken)f).InterfaceType == ChildGeneratingParentIdFilterNode.ParentFilterType);
                    
                    if (dataEntityToken != null)
                    {
                        referenceType = this.ChildGeneratingParentIdFilterNode.KeyPropertyInfo.DeclaringType;
                        referenceValue = this.ChildGeneratingParentIdFilterNode.KeyPropertyInfo.GetValue(dataEntityToken.Data, null);
                    }                    
                }
                else
                {
                    var treeDataFieldGroupingElementEntityToken = parentEntityToken as TreeDataFieldGroupingElementEntityToken;

                    referenceType = treeDataFieldGroupingElementEntityToken.ChildGeneratingDataElementsReferenceType;
                    referenceValue = treeDataFieldGroupingElementEntityToken.ChildGeneratingDataElementsReferenceValue;
                }
            }


            if (this.FolderRanges != null)
            {
                return CreateFolderRangeElements(parentEntityToken, referenceType, referenceValue, dynamicContext);
            }

            IEnumerable<object> objects;

            if (!this.LocalizationEndabled)
            {
                objects = GetObjects(dynamicContext);
            }
            else
            {
                List<object> orgObjects = GetObjects(dynamicContext);
                using (new DataScope(UserSettings.ForeignLocaleCultureInfo))
                {
                    List<object> foriegnObjects = GetObjects(dynamicContext);
                    orgObjects.AddRange(foriegnObjects);
                    orgObjects.Sort(); // TODO: Check if sorting here is necessary
                    objects = orgObjects.Distinct();
                }
            }

            if (this.FirstLetterOnly)
            {
                return CreateFirstLetterOnlyElements(parentEntityToken, referenceType, referenceValue, dynamicContext, objects);
            }

            return CreateSimpleElements(parentEntityToken, referenceType, referenceValue, dynamicContext, objects);
        }



        private IEnumerable<Element> CreateSimpleElements(EntityToken parentEntityToken, Type referenceType, object referenceValue, TreeNodeDynamicContext dynamicContext, IEnumerable<object> objects)
        {
            bool shouldBeSortedByLabel;
            Func<object, string> labelFunc = GetLabelFunction(out shouldBeSortedByLabel);

            if (shouldBeSortedByLabel)
            {
                objects = objects.Evaluate();
            }

            var objectsAndLabels = objects.Select(o => new { Object = o, Label = labelFunc(o)});

            if (shouldBeSortedByLabel)
            {
                objectsAndLabels = this.SortDirection == SortDirection.Ascending
                    ? objectsAndLabels.OrderBy(a => a.Label)
                    : objectsAndLabels.OrderByDescending(a => a.Label);
            }

            foreach (var pair in objectsAndLabels)
            {
                Element element = CreateElement(
                    parentEntityToken,
                    referenceType,
                    referenceValue,
                    dynamicContext,
                    pair.Label,
                    f => f.GroupingValues.Add(this.GroupingValuesFieldName, this.DateTimeFormater.Serialize(pair.Object))
                );

                yield return element;
            }
        }

        private Func<object, string> GetLabelFunction(out bool shouldBeSortedByLabel)
        {
            if (this.PropertyInfo.PropertyType == typeof(DateTime))
            {
                shouldBeSortedByLabel = false;
                return f => this.DateTimeFormater.FormatLabel(f);
            }
            
            var referenceProperties = DataAttributeFacade.GetDataReferenceProperties(PropertyInfo.DeclaringType);
            var referenceInfo = referenceProperties.FirstOrDefault(p => p.SourcePropertyName == this.PropertyInfo.Name);

            if (referenceInfo == null)
            {
                shouldBeSortedByLabel = false;
                return f => (f ?? "(NULL)").ToString();
            }

            shouldBeSortedByLabel = true;

            return key =>
            {
                if (key == null) return "(NULL)";

                IData reference = DataFacade.TryGetDataByUniqueKey(referenceInfo.TargetType, key);
                return reference != null ? reference.GetLabel() : key.ToString();
            };
        }

        private IEnumerable<Element> CreateFolderRangeElements(EntityToken parentEntityToken, Type referenceType, object referenceValue, TreeNodeDynamicContext dynamicContext)
        {
            IEnumerable<int> indexes;
            if (this.Display == LeafDisplayMode.Lazy)
            {
                indexes = new List<int>();
            }
            else
            {
                if (!this.LocalizationEndabled)
                {
                    indexes = GetObjects<int>(dynamicContext);
                }
                else
                {
                    List<int> orgObjects = GetObjects<int>(dynamicContext);
                    using (new DataScope(UserSettings.ForeignLocaleCultureInfo))
                    {
                        List<int> foreignObjects = GetObjects<int>(dynamicContext);
                        orgObjects.AddRange(foreignObjects);
                        orgObjects.Sort();
                        indexes = orgObjects.Distinct();
                    }
                }
            }


            foreach (IFolderRange folderRange in this.FolderRanges.Ranges)
            {
                bool contained = indexes.Contains(folderRange.Index);
                if ((this.Display == LeafDisplayMode.Compact) && (contained == false)) continue;

                Element element = CreateElement(
                    parentEntityToken,
                    referenceType,
                    referenceValue,
                    dynamicContext,
                    folderRange.Label,
                    f =>
                    {
                        f.FolderRangeValues.Add(this.FieldName, folderRange.Index);
                    }
                );

                if (this.Display == LeafDisplayMode.Auto)
                {
                    element.VisualData.HasChildren = contained;
                }

                yield return element;
            }
        }



        private IEnumerable<Element> CreateFirstLetterOnlyElements(EntityToken parentEntityToken, Type referenceType, object referenceValue, TreeNodeDynamicContext dynamicContext, IEnumerable<object> objects)
        {
            foreach (object obj in objects)
            {
                if (obj == null) continue;

                string stringObject = (string)obj;

                string label = stringObject;

                Element element = CreateElement(
                    parentEntityToken,
                    referenceType,
                    referenceValue, 
                    dynamicContext,
                    label,
                    f => f.GroupingValues.Add(this.GroupingValuesFieldName, label)
                );

                yield return element;
            }
        }



        private Element CreateElement(EntityToken parentEntityToken, Type referenceType, object referenceValue, TreeNodeDynamicContext dynamicContext, string label, Action<TreeDataFieldGroupingElementEntityToken> entityTokenAction)
        {
            TreeDataFieldGroupingElementEntityToken entityToken = new TreeDataFieldGroupingElementEntityToken(this.Id.ToString(), this.Tree.TreeId, this.SerializedInterfaceType);
            entityToken.GroupingValues = new Dictionary<string, object>(dynamicContext.FieldGroupingValues);
            entityToken.FolderRangeValues = new Dictionary<string, int>(dynamicContext.FieldFolderRangeValues);
            entityToken.ChildGeneratingDataElementsReferenceType = referenceType;
            entityToken.ChildGeneratingDataElementsReferenceValue = referenceValue;

            entityTokenAction(entityToken);

            Element element = new Element(new ElementHandle(
                dynamicContext.ElementProviderName,
                entityToken,
                dynamicContext.Piggybag.PreparePiggybag(this.ParentNode, parentEntityToken)
            ));

            element.VisualData = new ElementVisualizedData
            {
                Label = label,
                ToolTip = label,
                HasChildren = true,
                Icon = this.Icon,
                OpenedIcon = this.Icon
            };

            element.ElementExternalActionAdding = element.ElementExternalActionAdding.Remove(ElementExternalActionAdding.AllowManageUserPermissions);

            return element;
        }



        private Expression CreateSelectBodyExpression(ParameterExpression parameterExpression, Expression fieldExpression, out List<LambdaExpression> orderByExpressions)
        {
            orderByExpressions = null;

            if (this.FolderRanges != null)
            {
                return this.FolderRanges.CreateContainsListSelectBodyExpression(fieldExpression, parameterExpression);
            }
            
            if (this.PropertyInfo.PropertyType == typeof(DateTime))
            {
                List<Expression> parameters = new List<Expression>();
                if (this.DateTimeFormater.HasYear) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Year));
                if (this.DateTimeFormater.HasMonth) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Month));
                if (this.DateTimeFormater.HasDay) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Day));
                if (this.DateTimeFormater.HasHour) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Hour));
                if (this.DateTimeFormater.HasMinute) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Minute));
                if (this.DateTimeFormater.HasSecond) parameters.Add(Expression.Property(fieldExpression, DateTimeFormater.DateTime_Second));

                var members = this.DateTimeFormater.GetTupleMembers();
                Expression expression = Expression.New(this.DateTimeFormater.GetTupleConstructor(), parameters, members);

                if (parameters.Any())
                {
                    orderByExpressions = new List<LambdaExpression>();

                    // Ordering my date parts. Needed for LINQ2SQL to produce a correct query
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var newParameter = Expression.Parameter(expression.Type);

                        var newTypeProperty = Expression.Property(newParameter, members[i]);

                        var keySelector = Expression.Lambda(newTypeProperty, newParameter);

                        orderByExpressions.Add(keySelector);
                    }
                }
                
                return expression;
            }
            
            if (this.FirstLetterOnly)
            {
                Expression expression = Expression.Condition(
                    Expression.And(
                        Expression.NotEqual(
                            fieldExpression,
                            Expression.Constant(null)
                        ),
                        Expression.NotEqual(
                            fieldExpression,
                            Expression.Constant("")
                        )
                    ),
                    Expression.Call(
                        Expression.Call(
                            fieldExpression,
                            this.StringSubstringMethodInfo,
                            Expression.Constant(0),
                            Expression.Constant(1)
                        ),
                        this.ToUpperCompareMethodInfo
                    ),
                    fieldExpression,
                    typeof(string)
                );

                return expression;
            }
            
            return fieldExpression;
        }



        private Expression GetObjectsExpression(TreeNodeDynamicContext dynamicContext, bool includeAllGroupingFields = false)
        {
            ParameterExpression parameterExpression = Expression.Parameter(this.InterfaceType, "data");

            Expression filterExpression = CreateAccumulatedFilterExpression(parameterExpression, this.InterfaceType, dynamicContext);

            Expression whereExpression = ExpressionHelper.CreateWhereExpression(DataFacade.GetData(this.InterfaceType).Expression, parameterExpression, filterExpression);

            Expression fieldExpression = ExpressionHelper.CreatePropertyExpression(this.InterfaceType, this.PropertyInfo.DeclaringType, this.FieldName, parameterExpression);

            var keySelector = Expression.Lambda(fieldExpression, parameterExpression);
            Expression orderByExpression = this.SortDirection == SortDirection.Ascending
                ? ExpressionHelper.CreateOrderByExpression(whereExpression, keySelector)
                : ExpressionHelper.CreateOrderByDescendingExpression(whereExpression, keySelector);

            Expression selectBodyExpression;

            List<LambdaExpression> orderByExpressions = null;

            if (!includeAllGroupingFields)
            {
                selectBodyExpression = CreateSelectBodyExpression(parameterExpression, fieldExpression, out orderByExpressions);
            }
            else
            {
                List<Expression> bodyExpressions = new List<Expression>();
                foreach (DataFolderElementsTreeNode dataFolderElementsTreeNode in this.AllGroupingNodes)
                {
                    List<LambdaExpression> innerOrderByExpressions;

                    Expression groupFieldExpression = ExpressionHelper.CreatePropertyExpression(dataFolderElementsTreeNode.InterfaceType, dataFolderElementsTreeNode.PropertyInfo.DeclaringType, dataFolderElementsTreeNode.FieldName, parameterExpression);
                    Expression bodyExpression = dataFolderElementsTreeNode.CreateSelectBodyExpression(parameterExpression, groupFieldExpression, out innerOrderByExpressions);

                    bodyExpressions.Add(bodyExpression);
                }

                selectBodyExpression = Expression.New(this.AllGroupingsTupleConstructor, bodyExpressions);

                // TODO: proper processing of innerOrderByExpressions
            }

            Expression selectExpression = ExpressionHelper.CreateSelectExpression(
                    orderByExpression,
                    selectBodyExpression,
                    parameterExpression);

            Expression distinctExpression = ExpressionHelper.CreateDistinctExpression(selectExpression);


            // Sorting after calling "DISTINCT" to fix LINQ2SQL issue. No need to sort while resolving security
            if (orderByExpressions != null && dynamicContext.Direction == TreeNodeDynamicContextDirection.Down)
            {
                distinctExpression = ApplyOrder(distinctExpression, orderByExpressions);
            }

            return distinctExpression;
        }


        private Expression ApplyOrder(Expression expression, List<LambdaExpression> fieldSelectors)
        {
            for (int i = 0; i < fieldSelectors.Count; i++)
            {
                if (i == 0)
                {
                    expression = this.SortDirection == SortDirection.Ascending
                        ? ExpressionHelper.CreateOrderByExpression(expression, fieldSelectors[i])
                        : ExpressionHelper.CreateOrderByDescendingExpression(expression,fieldSelectors[i]);
                }
                else
                {
                    expression = this.SortDirection == SortDirection.Ascending
                        ? ExpressionHelper.ThenByExpression(expression, fieldSelectors[i])
                        : ExpressionHelper.ThenByDescendingExpression(expression, fieldSelectors[i]);
                }
            }

            return expression;
        }

        private List<object> GetObjects(TreeNodeDynamicContext dynamicContext, bool includeAllGroupingFields = false)
        {
            Expression expression = GetObjectsExpression(dynamicContext, includeAllGroupingFields);

            return ExpressionHelper.GetObjects(this.InterfaceType, expression);
        }



        private List<T> GetObjects<T>(TreeNodeDynamicContext dynamicContext)
        {
            Expression expression = GetObjectsExpression(dynamicContext);

            return ExpressionHelper.GetCastedObjects<T>(this.InterfaceType, expression);
        }

        

        private bool UseChildGeneratingFilterExpression
        {
            get
            {
                return this.IsTopFolderParent && this.Display != LeafDisplayMode.Lazy;
            }
        }



        internal override Expression CreateFilterExpression(ParameterExpression parameterExpression, TreeNodeDynamicContext dynamicContext, IList<int> filtersToSkip = null)
        {
            DataEntityToken dataEntityToken = dynamicContext.CurrentEntityToken as DataEntityToken;
            TreeSimpleElementEntityToken treeSimpleElementEntityToken = dynamicContext.CurrentEntityToken as TreeSimpleElementEntityToken;

            Expression fieldExpression = ExpressionHelper.CreatePropertyExpression(this.InterfaceType, this.PropertyInfo.DeclaringType, this.FieldName, parameterExpression);

            object value;

            Func<Expression> resultFilterExpressionFactory = () =>
                {
                    return this.UseChildGeneratingFilterExpression
                               ? this.ChildGeneratingDataElementsTreeNode.CreateFilterExpression(parameterExpression, dynamicContext)
                               : null;
            };


            if (this.FolderRanges == null)
            {
                if (dynamicContext.FieldGroupingValues.ContainsKey(this.GroupingValuesFieldName))
                {
                    value = dynamicContext.FieldGroupingValues[this.GroupingValuesFieldName];
                }
                else if (dataEntityToken != null)
                {
                    if (CreateFilterExpression_GetValueFromDataEntityToken(dataEntityToken, out value) == false)
                    {
                        return resultFilterExpressionFactory();
                    }
                }
                else if (treeSimpleElementEntityToken != null 
                         && treeSimpleElementEntityToken.ParentEntityToken is DataEntityToken)
                {
                    DataEntityToken parentDataEntityToken = treeSimpleElementEntityToken.ParentEntityToken as DataEntityToken;

                    if (CreateFilterExpression_GetValueFromDataEntityToken(parentDataEntityToken, out value) == false)
                    {
                        return resultFilterExpressionFactory();
                    }
                }
                else if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Down)
                {
                    // We shall not create filter for our self when undfolding 

                    return resultFilterExpressionFactory();
                }
                else if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Up)
                {
                    // At this point we are going upwards, building the filter and one or 
                    // more of the parent elements has not been oppened, so we are not able to 
                    // create a filter. 
                    // This will happen if a parent filter is below us
                    return null;
                }
                else
                {
                    // This will only happen if we are searcning up and are given another entity token that
                    // TreeDataFieldGroupingElementEntityToken and DataEntityToken or TreeSimpleElementEntityToken 
                    throw new NotImplementedException(string.Format("Unsupported child entity token type '{0}'", dynamicContext.CurrentEntityToken.GetType()));
                }
            }
            else
            {
                if (dynamicContext.FieldFolderRangeValues.ContainsKey(this.FieldName))
                {
                    value = dynamicContext.FieldFolderRangeValues[this.FieldName];
                }
                else if (dataEntityToken != null)
                {
                    if (CreateFilterExpression_GetFolderIndexFromDataEntityToken(dataEntityToken, parameterExpression, fieldExpression, out value) == false)
                    {
                        return resultFilterExpressionFactory();
                    }
                }
                else if (treeSimpleElementEntityToken != null 
                        && treeSimpleElementEntityToken.ParentEntityToken is DataEntityToken)
                {
                    DataEntityToken parentDataEntityToken = treeSimpleElementEntityToken.ParentEntityToken as DataEntityToken;

                    if (CreateFilterExpression_GetFolderIndexFromDataEntityToken(parentDataEntityToken, parameterExpression, fieldExpression, out value) == false)
                    {
                        return resultFilterExpressionFactory();
                    }
                }
                else if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Down)
                {
                    // We shall not create filter for our self when undfolding 
                    return resultFilterExpressionFactory();
                }
                else if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Up)
                {
                    // At this point we are going upwards, building the filter and one or 
                    // more of the parent elements has not been oppened, so we are not able to 
                    // create a filter. 
                    // This will happen if a parent filter is below us
                    return null;
                }
                else
                {
                    // This will only happen if we are searcning up and are given another entity token that
                    // TreeDataFieldGroupingElementEntityToken and DataEntityToken or TreeSimpleElementEntityToken 
                    throw new NotImplementedException(string.Format("Unsupported child entity token type '{0}'", dynamicContext.CurrentEntityToken.GetType()));
                }
            }

            Expression filterExpression;
            if (this.FolderRanges != null)
            {
                int folderRangeIndex = (int)value;

                filterExpression = CreateFolderRangeFilterExpression(folderRangeIndex, fieldExpression);
            }
            else if (this.FirstLetterOnly)
            {
                filterExpression = CreateFirstLetterOnlyFilterExpression(value, fieldExpression);
            }
            else
            {
                filterExpression = CreateSimpleFilterExpression(value, fieldExpression);
            }

            if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Down)
            {
                if (this.UseChildGeneratingFilterExpression)
                {
                    Expression childFilerExpression = this.ChildGeneratingDataElementsTreeNode.CreateFilterExpression(parameterExpression, dynamicContext);

                    filterExpression = filterExpression.NestedAnd(childFilerExpression);
                }
            }
            
            return filterExpression;
        }



        private bool CreateFilterExpression_GetValueFromDataEntityToken(DataEntityToken dataEntityToken, out object value)
        {
            if (dataEntityToken.InterfaceType != this.InterfaceType)
            {
                // If we dont have the grouping/folerrange value from entity token and dont have the 
                // right data item to get the value, we are not able to create a filter.
                value = null;
                return false;
            }

            IData data = dataEntityToken.Data;

            value = this.PropertyInfo.GetValue(data, null);
            value = ConvertFieldValue(this, value);

            return true;
        }



        private bool CreateFilterExpression_GetFolderIndexFromDataEntityToken(DataEntityToken dataEntityToken, ParameterExpression parameterExpression, Expression fieldExpression, out object value)
        {
            if (dataEntityToken.InterfaceType != this.InterfaceType)
            {
                // If we dont have the foler range value from entity token and dont have the 
                // right data item to get the value, we are not able to create a filter.
                value = null;
                return false;
            }

            this.FolderIndexListClearMethodInfo.Invoke(this.FolderIndexListObject, null);
            this.FolderIndexListAddMethodInfo.Invoke(this.FolderIndexListObject, new object[] { dataEntityToken.Data });

            Expression selectBodyExpression = this.FolderRanges.CreateContainsListSelectBodyExpression(fieldExpression, parameterExpression);

            Expression selectExpression = ExpressionHelper.CreateSelectExpression(this.FolderIndexListQueryable.Expression, selectBodyExpression, parameterExpression);

            value = this.FolderIndexListQueryable.Provider.CreateQuery(selectExpression).ToEnumerableOfObjects().Single();

            return true;
        }



        private Expression CreateSimpleFilterExpression(object value, Expression fieldExpression)
        {
            if ((this.PropertyInfo.PropertyType != typeof(DateTime)) && (this.PropertyInfo.PropertyType != typeof(DateTime?)))
            {
                Expression expression = Expression.Equal(fieldExpression, Expression.Constant(value, this.PropertyInfo.PropertyType));

                return expression;
            }


            DateTime dateTime = DateTimeFormater.Deserialize(value.ToString());

            if (this.DateFormat == null)
            {
                Expression expression = Expression.Equal(fieldExpression, Expression.Constant(dateTime));

                return expression;
            }


            Expression currentExpression = null;

            if (this.DateTimeFormater.HasYear) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Year, dateTime.Year);
            if (this.DateTimeFormater.HasMonth) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Month, dateTime.Month);
            if (this.DateTimeFormater.HasDay) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Day, dateTime.Day);
            if (this.DateTimeFormater.HasHour) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Hour, dateTime.Hour);
            if (this.DateTimeFormater.HasMinute) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Minute, dateTime.Minute);
            if (this.DateTimeFormater.HasSecond) currentExpression = AddEqualsExpression(fieldExpression, currentExpression, DateTimeFormater.DateTime_Second, dateTime.Second);

            return currentExpression;
        }



        private Expression CreateFolderRangeFilterExpression(int folderRangeIndex, Expression fieldExpression)
        {
            Expression filterExpression = this.FolderRanges.CreateFilterExpression(folderRangeIndex, fieldExpression);

            return filterExpression;
        }



        private Expression CreateFirstLetterOnlyFilterExpression(object value, Expression fieldExpression)
        {
            string castedValue = (string)value;

            if (castedValue != "")
            {
                Expression expression = Expression.Condition(
                    Expression.NotEqual(
                        fieldExpression,
                        Expression.Constant(null)
                    ),
                    Expression.Call(
                        Expression.Call(
                            fieldExpression,
                            this.ToUpperCompareMethodInfo
                        ),
                        this.StringStartsWithMethodInfo,
                        Expression.Constant(castedValue)
                    ),
                    Expression.Constant(false)
                );

                return expression;
            }
            else
            {
                Expression expression =
                    Expression.Equal(
                        fieldExpression,
                        Expression.Constant("")
                    );

                return expression;
            }
        }



        private Expression AddEqualsExpression(Expression fieldExpression, Expression currentExpression, PropertyInfo propertyInfo, int value)
        {
            return currentExpression.NestedAnd(
                    Expression.Equal(
                        Expression.Property(fieldExpression, propertyInfo),
                        Expression.Constant(value)
                    )
                );
        }



        private static object ConvertFieldValue(DataFolderElementsTreeNode dataFolderElementsTreeNode, object fieldValue)
        {
            if (dataFolderElementsTreeNode.FirstLetterOnly)
            {
                string stringFieldValue = (string)fieldValue;

                return stringFieldValue.IsNullOrEmpty() ? "" : stringFieldValue.Substring(0, 1);
            }

            if (dataFolderElementsTreeNode.DateFormat != null)
            {
                if (fieldValue is DateTime)
                {
                    return dataFolderElementsTreeNode.DateTimeFormater.SerializeDateTime((DateTime)fieldValue);
                }
                
                dataFolderElementsTreeNode.DateTimeFormater.GetDateTime(fieldValue);
                return dataFolderElementsTreeNode.DateTimeFormater.Serialize(fieldValue);
            }

            return fieldValue;
        }



        /// <exclude />
        protected override void OnInitialize()
        {
            DataFolderElementsTreeNode parentNode = this.ParentNode as DataFolderElementsTreeNode;

            if (this.InterfaceType == null)
            {
                if (parentNode == null)
                {
                    AddValidationError("TreeValidationError.DataFolderElements.MissingInterfaceType");
                    return;
                }

                this.InterfaceType = parentNode.InterfaceType;
            }
            else
            {
                if (typeof(IData).IsAssignableFrom(this.InterfaceType) == false)
                {
                    AddValidationError("TreeValidationError.Common.NotImplementingIData", this.InterfaceType, typeof(IData));
                    return;
                }

                if ((parentNode != null) && (parentNode.InterfaceType != this.InterfaceType))
                {
                    AddValidationError("TreeValidationError.DataFolderElements.WrongInterfaceType", this.InterfaceType, parentNode.InterfaceType);
                    return;
                }
            }


            this.PropertyInfo = this.InterfaceType.GetPropertiesRecursively().SingleOrDefault(f => f.Name == this.FieldName);
            if (this.PropertyInfo == null)
            {
                AddValidationError("TreeValidationError.Common.MissingProperty", this.InterfaceType, this.FieldName);
                return;
            }


            
            if ((this.DateFormat != null) && (this.PropertyInfo.PropertyType != typeof(DateTime)))
            {
                AddValidationError("TreeValidationError.DataFolderElements.DateFormetNotAllowed", this.FieldName, typeof(DateTime), this.PropertyInfo.PropertyType);
            }

            if ((this.PropertyInfo.PropertyType == typeof(DateTime)) && (this.DateFormat == null))
            {
                AddValidationError("TreeValidationError.DataFolderElements.DateFormetIsMissing", this.FieldName);
                return;
            }


            this.DateTimeFormater = new DateTimeFormater(this.DateFormat);


            if ((string.IsNullOrEmpty(this.Range) == false) && this.FirstLetterOnly)
            {
                AddValidationError("TreeValidationError.DataFolderElements.RangesAndFirstLetterOnlyNotAllowed");
            }

            if (this.FirstLetterOnly && this.PropertyInfo.PropertyType != typeof(string))
            {
                AddValidationError("TreeValidationError.DataFolderElements.WrongFirstLetterOnlyPropertyType", this.FieldName, typeof(string), this.PropertyInfo.PropertyType);
            }

            if (string.IsNullOrEmpty(this.Range) == false)
            {
                this.FolderRanges = FolderRangesCreator.Create(this, this.Range, this.FieldName, this.PropertyInfo.PropertyType);
            }

            if (this.ChildNodes.OfType<DataElementsTreeNode>().Any(f => f.InterfaceType != this.InterfaceType))
            {
                AddValidationError("TreeValidationError.DataFolderElements.WrongDateChildInterfaceType", this.InterfaceType);
            }

            this.SerializedInterfaceType = TypeManager.SerializeType(this.InterfaceType);


            List<string> usedPropertyNames = new List<string>();
            this.AllGroupingNodes = new List<DataFolderElementsTreeNode>();
            List<Type> fieldTypes = new List<Type>();
            DataFolderElementsTreeNode treeNode = this;
            int dataFolderElementCount = 0;
            while (treeNode != null)
            {
                dataFolderElementCount++;
                if (treeNode.InterfaceType != this.InterfaceType)
                {
                    AddValidationError("TreeValidationError.DataFolderElements.InterfaceTypeSwitchNotAllowed", treeNode.InterfaceType, this.InterfaceType);
                    break;
                }

                if (treeNode.PropertyInfo.PropertyType != typeof(DateTime))
                {
                    if (usedPropertyNames.Contains(treeNode.FieldName))
                    {
                        AddValidationError("TreeValidationError.DataFolderElements.SameFieldUsedTwice", treeNode.FieldName);
                        break;
                    }
                    
                    usedPropertyNames.Add(treeNode.FieldName);
                }

                this.AllGroupingNodes.Add(treeNode);

                if (treeNode.FolderRanges != null)
                {
                    fieldTypes.Add(typeof(int));
                }
                else if (treeNode.PropertyInfo.PropertyType == typeof(DateTime))
                {
                    fieldTypes.Add(treeNode.DateTimeFormater.GetTupleConstructor().DeclaringType);
                }
                else
                {
                    fieldTypes.Add(treeNode.PropertyInfo.PropertyType);
                }

                treeNode = treeNode.ParentNode as DataFolderElementsTreeNode;
            }

            Type type = GetTupleType(this.AllGroupingNodes.Count);
            type = type.MakeGenericType(fieldTypes.ToArray());

            this.AllGroupingsTupleConstructor = type.GetConstructors().Single();


            Type listType = typeof(List<>).MakeGenericType(this.InterfaceType);
            this.FolderIndexListAddMethodInfo = listType.GetMethods().Single(f => f.Name == "Add");
            this.FolderIndexListClearMethodInfo = listType.GetMethods().Single(f => f.Name == "Clear");

            this.FolderIndexListObject = Activator.CreateInstance(listType);
            this.FolderIndexListQueryable = ((IEnumerable)this.FolderIndexListObject).AsQueryable();


            if (this.FieldName == null)
            {
                this.GroupingValuesFieldName = this.FieldName;
            }
            else
            {
                this.GroupingValuesFieldName = string.Format("■{0}_{1}", this.FieldName, dataFolderElementCount);
            }

          
            this.IsTopFolderParent = (this.ParentNode is DataFolderElementsTreeNode) == false;
            this.ChildGeneratingDataElementsTreeNode = this.DescendantsBreadthFirst().OfType<DataElementsTreeNode>().First();
            IEnumerable<ParentIdFilterNode> parentIdFilterNodes = this.ChildGeneratingDataElementsTreeNode.FilterNodes.OfType<ParentIdFilterNode>();
            if (parentIdFilterNodes.Count() <= 1)
            {
                this.ChildGeneratingParentIdFilterNode = this.ChildGeneratingDataElementsTreeNode.FilterNodes.OfType<ParentIdFilterNode>().SingleOrDefault();
            }
            else
            {
                AddValidationError("TreeValidationError.DataFolderElements.TooManyParentIdFilters", treeNode.FieldName);
            }

            this.KeyPropertyInfo = this.InterfaceType.GetKeyProperties()[0];

            if (typeof(ILocalizedControlled).IsAssignableFrom(this.InterfaceType) == false)
            {
                this.ShowForeignItems = false;
            }
        }



        private bool LocalizationEndabled
        {
            get
            {
                return this.ShowForeignItems && !UserSettings.ActiveLocaleCultureInfo.Equals(UserSettings.ForeignLocaleCultureInfo);
            }
        }



        private static Type GetTupleType(int filedCount)
        {
            switch (filedCount)
            {
                case 1:
                    return TupleType1;

                case 2:
                    return TupleType2;

                case 3:
                    return TupleType3;

                case 4:
                    return TupleType4;

                case 5:
                    return TupleType5;

                case 6:
                    return TupleType6;

                default:
                    throw new NotImplementedException();
            }
        }
        private static Type TupleType1 = typeof(Tuple<>);
        private static Type TupleType2 = typeof(Tuple<,>);
        private static Type TupleType3 = typeof(Tuple<,,>);
        private static Type TupleType4 = typeof(Tuple<,,,>);
        private static Type TupleType5 = typeof(Tuple<,,,,>);
        private static Type TupleType6 = typeof(Tuple<,,,,,>);



        /// <exclude />
        public override string ToString()
        {
            return string.Format("DataFolderElementsTreeNode, Id = {0}, DataType = {1}, FieldName = {2}, {3}", this.Id, this.InterfaceType, this.FieldName, this.ParentString());
        }
    }
}
