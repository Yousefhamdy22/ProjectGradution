﻿namespace DAL.Models
{
    public class DetectedObject
    {

        public string Label { get; set; }
        public float Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; }
    }
}
