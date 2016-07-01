using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DotNetDETs.Infrastructure
{
    public class Metadata
    {
        public string field_name { get; set; }
        public string form_name { get; set; }
        public string section_header { get; set; }
        public string field_type { get; set; }
        public string field_label { get; set; }
        public string select_choices_or_calculations { get; set; }
        public string field_note { get; set; }
        public string text_validation_type_or_show_slider_number { get; set; }
        public string text_validation_min { get; set; }
        public string text_validation_max { get; set; }
        public string identifier { get; set; }
        public string branching_logic { get; set; }
        public string required_field { get; set; }
        public string custom_alignment { get; set; }
        public string question_number { get; set; }
        public string matrix_group_name { get; set; }
        public string matrix_ranking { get; set; }
        public string field_annotation { get; set; }
    }

}