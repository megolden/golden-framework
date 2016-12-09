﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Golden.Tests
{
	public partial class Student
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual int? CityRef { get; set; }
		public virtual DateTime? BirthDate { get; set; }
		public virtual City City { get; set; }
	}

	public partial class City
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual ICollection<Student> Student { get; set; }
		public City()
		{
			Student = new HashSet<Student>();
		}
	}

	public partial class Province
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
	}

	public partial class StudentView
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string CityName { get; set; }
	}

	public partial class fnGetNamesResult
	{
		public virtual string Name { get; set; }
	}

	public partial class spGetNamesResult
	{
		public virtual string Name { get; set; }
	}

	public partial class fnGetStudentsResult
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
	}

	public partial class spGetStudentsResult
	{
		public virtual int? Id { get; set; }
		public virtual string Name { get; set; }
	}

	public partial class spGetNamesAndCityNamesResult
	{
		public virtual string Name { get; set; }
	}

	public partial class spGetNamesAndCityNamesResult2
	{
		public virtual string CityName { get; set; }
	}
}