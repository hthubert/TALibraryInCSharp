/* 
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace TaLib
{
    [XmlRoot(ElementName = "RequiredInputArgument")]
    public class RequiredInputArgument
    {
        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "RequiredInputArguments")]
    public class RequiredInputArguments
    {
        [XmlElement(ElementName = "RequiredInputArgument")]
        public List<RequiredInputArgument> RequiredInputArgument { get; set; }
    }

    [XmlRoot(ElementName = "Flags")]
    public class Flags
    {
        [XmlElement(ElementName = "Flag")]
        public List<string> Flag { get; set; }
    }

    [XmlRoot(ElementName = "OutputArgument")]
    public class OutputArgument
    {
        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Flags")]
        public Flags Flags { get; set; }
    }

    [XmlRoot(ElementName = "OutputArguments")]
    public class OutputArguments
    {
        [XmlElement(ElementName = "OutputArgument")]
        public List<OutputArgument> OutputArgument { get; set; }
    }

    [XmlRoot(ElementName = "FinancialFunction")]
    public class FinancialFunction
    {
        [XmlElement(ElementName = "Abbreviation")]
        public string Abbreviation { get; set; }
        [XmlElement(ElementName = "CamelCaseName")]
        public string CamelCaseName { get; set; }
        [XmlElement(ElementName = "ShortDescription")]
        public string ShortDescription { get; set; }
        [XmlElement(ElementName = "GroupId")]
        public string GroupId { get; set; }
        [XmlElement(ElementName = "RequiredInputArguments")]
        public RequiredInputArguments RequiredInputArguments { get; set; }
        [XmlElement(ElementName = "OutputArguments")]
        public OutputArguments OutputArguments { get; set; }
        [XmlElement(ElementName = "OptionalInputArguments")]
        public OptionalInputArguments OptionalInputArguments { get; set; }
        [XmlElement(ElementName = "Flags")]
        public Flags Flags { get; set; }
    }

    [XmlRoot(ElementName = "Range")]
    public class Range
    {
        [XmlElement(ElementName = "Minimum")]
        public string Minimum { get; set; }
        [XmlElement(ElementName = "Maximum")]
        public string Maximum { get; set; }
        [XmlElement(ElementName = "SuggestedStart")]
        public string SuggestedStart { get; set; }
        [XmlElement(ElementName = "SuggestedEnd")]
        public string SuggestedEnd { get; set; }
        [XmlElement(ElementName = "SuggestedIncrement")]
        public string SuggestedIncrement { get; set; }
        [XmlElement(ElementName = "Precision")]
        public string Precision { get; set; }
    }

    [XmlRoot(ElementName = "OptionalInputArgument")]
    public class OptionalInputArgument
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "ShortDescription")]
        public string ShortDescription { get; set; }
        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "Range")]
        public Range Range { get; set; }
        [XmlElement(ElementName = "DefaultValue")]
        public string DefaultValue { get; set; }
    }

    [XmlRoot(ElementName = "OptionalInputArguments")]
    public class OptionalInputArguments
    {
        [XmlElement(ElementName = "OptionalInputArgument")]
        public List<OptionalInputArgument> OptionalInputArgument { get; set; }
    }

    [XmlRoot(ElementName = "FinancialFunctions")]
    public class FinancialFunctions
    {
        [XmlElement(ElementName = "FinancialFunction")]
        public List<FinancialFunction> FinancialFunction { get; set; }
    }
}
