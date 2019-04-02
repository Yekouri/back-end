﻿namespace PolloPollo.Shared
{
    public class ProductDTO
    {
        public int ProductId { get; set; }

        public string Title { get; set; }

        public int UserId { get; set; }

        public int Price { get; set; }

        public string Description { get; set; }

        public string Country { get; set; }

        public string Location { get; set; }

        public bool Available { get; set; }

        public string Thumbnail { get; set; }
    }
}
