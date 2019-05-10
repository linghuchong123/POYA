﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace POYA.Areas.XUserFile.Models
{
    public class LUserFile : FileDirCommon  //  ViewModel
    {
        public string MD5 { get; set; } = string.Empty;

        /// <summary>
        /// The default value is DateTimeOffset.Now
        /// </summary>
        [Display(Name = "Date")]
        public DateTimeOffset DOCreate { get; set; } = DateTimeOffset.Now;
        public bool IsLegal { get; set; } = true;

        #region DEPOLLUTION
        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public string ContentType { get; set; }

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public IFormFile LFormFile { get; set; }
        #endregion
    }

    public class LDir : FileDirCommon
    {
        /// <summary>
        /// The default value is DateTimeOffset.Now
        /// </summary>
        [Display(Name = "Date")]
        public DateTimeOffset DOCreate { get; set; } = DateTimeOffset.Now;

    }


    public class LFile
    {
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// The id of the first user upload this file
        /// </summary>
        public string UserId { get; set; }

        public DateTimeOffset DOUpload { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// The MD5 string, it equal the file name in specified directory
        /// </summary>
        public string MD5 { get; set; }
    }

    #region DEPOLLUTION

    public class LUserSharingFile   //  BEGIN IT AFTER COPYING DIR FUNCATION IS FINISH
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FileId { get; set; }
        public string SharingCode { get; set; }
            = new Random().Next(100000, 999999).ToString();
        public string Comment { get; set; }
    }

    public class ID8InDirId
    {
        public Guid Id { get; set; }
        public Guid InDirId { get; set; }
    }

    public class ID8NewID
    {
        public Guid NewId { get; set; }
        //  public Guid NewInDirId { get; set; }
        public Guid OriginalId { get; set; }
        public Guid OriginalInDirId { get; set; }
    }


    public class FileDirCommon
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; }

        /// <summary>
        /// The default value is Guid.Empty
        /// </summary>
        public Guid InDirId { get; set; } = Guid.Empty;

        [Display(Name = "Name")]
        public string Name { get; set; }

        #region SHARING

        /// <summary>
        /// Gets or sets a flag indicating if <see cref="LUserFile"/> or <see cref="LDir"/>. Defaults to true
        /// </summary>
        [Display(Name="Is shared")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// Gets or sets the code of shared <see cref="LUserFile"/>, defaults to [100_000, 999_999]
        /// </summary>
        [Display(Name="Sharing code")]
        [StringLength(maximumLength:50,MinimumLength =6)]
        public string SharingCode { get; set; }

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public List<SelectListItem> IsSharedSelectListItems { get; set; }
        #endregion

        #region DEPOLLUTION

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public string InDirName { get; set; }

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public string InFullPath { get; set; } = "root";

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public List<SelectListItem> UserAllSubDirSelectListItems { get; set; }

        /// <summary>
        /// [NotMapped]
        /// Determine is coping(is 1) file to directory or moving(is 2), do nothing if it is 0
        /// </summary>
        [NotMapped]
        public CopyMove CopyMove { get; set; } = CopyMove.DoNoThing;

        /// <summary>
        /// [NotMapped]
        /// </summary>
        [NotMapped]
        public List<SelectListItem> CopyMoveSelectListItems { get; set; }

        #endregion

    }

    public enum CopyMove {
        Copy = 1,
        Move = 2,
        DoNoThing = 0
    };

    public class MediaType
    {
        //  Name,Template,Reference
        public string Name { get; set; }
        public string Template { get; set; }
        public string Reference { get; set; }
    }

    public class ContrastMD5
    {
        public Guid InDirId { get; set; } = Guid.Empty;
        public IEnumerable<File8MD5> File8MD5s { get; set; }
    }

    public class File8MD5
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string MD5 { get; set; }
    }

    public class LFilePost
    {
        /// <summary>
        /// The id for callback
        /// </summary>
        public int Id { get; set; }
        public IFormFile _LFile { get; set; }
        public Guid InDirId { get; set; }
    }
    #endregion

}

