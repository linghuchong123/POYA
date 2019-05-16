﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace POYA.Areas.XUserFile.Models
{
    public class LSharing   //  File   //  ViewModel
    {
        [Display(Name = "Give it a id for sharing")]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid LUserFileOrDirId { get; set; }

        [StringLength(maximumLength: 50)]
        [Display(Name = "Say to receiver")]
        public string Comment { get; set; }

        public bool IsLegal { get; set; } = true;

        #region DEPOLLUTION

        [NotMapped]
        public LUserFile LUserFile_ { get; set; }

        [NotMapped]
        public LDir LDir_ { get; set; }

        [NotMapped]
        public bool IsFile_ { get; set; }

        [NotMapped]
        [Display(Name = "Name")]
        public string LUserFileOrDirName_ { get; set; }

        #endregion
    }

    /// <summary>
    /// A copy will be made when you share a directory, and the mapping relation will be recorded by <see cref="LSharedDirMap"/>
    /// </summary>
    public class LSharingDirMap
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// The id of shared file or directory
        /// </summary>
        public Guid NewSharingId { get; set; } = Guid.NewGuid();
        //  public Guid NewInDirId { get; set; }
        /// <summary>
        /// The orginal id of shared file or directory
        /// </summary>
        public Guid OrginalId { get; set; }
        public Guid OrginalInDirId { get; set; }
    }
}