using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class CourseTranslation : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ShortDescription { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>Список пунктов "чему вы научитесь", хранится как JSON-массив строк.</summary>
        public string WhatYouWillLearn { get; set; } = "[]";
    }

}
