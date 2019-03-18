﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations; 

namespace PolloPollo.Shared
{
    class ReceiverCreateUpdateDTO
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}