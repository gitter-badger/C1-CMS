﻿using System;
using Composite.Core.Serialization;

namespace Composite.Data
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [SerializerHandler(typeof(DataSerializerHandler))]
    [KeyPropertyName("Id")]    
    public interface IPageData : IData
    {
        /// <exclude />
        [StoreFieldType(PhysicalStoreFieldType.Guid)]
        [ImmutableFieldId("{F6DF85E4-C577-49E5-ACD9-8BE8958736D6}")]
        Guid Id { get; set; }



        /// <exclude />
        [ForeignKey(typeof(Composite.Data.Types.IPage), "Id", AllowCascadeDeletes = true)]
        [StoreFieldType(PhysicalStoreFieldType.Guid)]
        [ImmutableFieldId("{F641EC01-75BB-49EC-B02A-969D6BE59A5F}")]
        Guid PageId { get; set; }
    }
}