using System;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Ignist.Models
{
	public class publications
	{

        public int Id { get; set; }
		public required string Tittle  { get; set; }
		public required string Contetn { get; set; }
		public DateTime CreatedAT { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}

