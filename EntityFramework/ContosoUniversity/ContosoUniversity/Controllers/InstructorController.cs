﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.Models;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private SchoolContext db = new SchoolContext();

        //
        // GET: /Instructor/

        
        public ActionResult Index(Int32? id, Int32? courseID)
        {
            var viewModel = new InstructorIndexData
                {
                    Instructors = db.Instructors
                                    .Include(i => i.OfficeAssignment)
                                    .Include(i => i.Courses.Select(c => c.Department))
                                    .OrderBy(i => i.LastName)
                };

            if (id != null)
            {
                ViewBag.InstructorID = id.Value;
                viewModel.Courses = viewModel.Instructors.Single(i => i.Id == id.Value).Courses;
            }


            if (courseID != null)
            {
                ViewBag.CourseID = courseID.Value;

                var selectedCourse = viewModel.Courses.Single(x => x.Id == courseID);
                db.Entry(selectedCourse).Collection(x => x.Enrollments).Load(); // explicit loading
                
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    db.Entry(enrollment).Reference(x => x.Student).Load(); // explicit loading
                }

                viewModel.Enrollments = selectedCourse.Enrollments;
            }

            return View(viewModel);
        }

        //
        // GET: /Instructor/Details/5

        public ActionResult Details(int id = 0)
        {
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        //
        // GET: /Instructor/Create

        public ActionResult Create()
        {
            ViewBag.Id = new SelectList(db.OfficeAssignments, "InstructorId", "Location");
            return View();
        }

        //
        // POST: /Instructor/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                db.Instructors.Add(instructor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Id = new SelectList(db.OfficeAssignments, "InstructorId", "Location", instructor.Id);
            return View(instructor);
        }

        //
        // GET: /Instructor/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Instructor instructor = db.Instructors
               .Include(i => i.OfficeAssignment) // Eager loading
               .Include(i => i.Courses)
               .Where(i => i.Id == id)
               .Single();
            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }

        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var allCourses = db.Courses;
            var instructorCourses = new HashSet<int>(instructor.Courses.Select(c => c.Id));
            var viewModel = new List<AssignedCourseData>();
            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.Id,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.Id)
                });
            }
            ViewBag.Courses = viewModel;
        }

        //
        // POST: /Instructor/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection formCollection, string[] selectedCourses)
        {
            var instructorToUpdate = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .Where(i => i.Id == id)
                .Single();

            if (TryUpdateModel(instructorToUpdate, "", null, new[] { "Courses" } /* Exclude properties */))
            {
                try
                {
                    // Handle OfficeAssignment update
                    if (string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment.Location))
                    {
                        instructorToUpdate.OfficeAssignment = null;
                    }

                    UpdateInstructorCourses(selectedCourses, instructorToUpdate);

                    db.Entry(instructorToUpdate).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (DataException)
                {
                    //Log the error (add a variable name after DataException)
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    return View();
                }
            }

            PopulateAssignedCourseData(instructorToUpdate);

            return View(instructorToUpdate);
        }

        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            if (selectedCourses == null) // No assigned courses
            {
                instructorToUpdate.Courses = new List<Course>();
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructorToUpdate.Courses.Select(c => c.Id));

            foreach (var course in db.Courses)
            {
                if (selectedCoursesHS.Contains(course.Id.ToString()))
                {
                    if (!instructorCourses.Contains(course.Id))
                    {
                        instructorToUpdate.Courses.Add(course);
                    }
                }
                else
                {
                    if (instructorCourses.Contains(course.Id))
                    {
                        instructorToUpdate.Courses.Remove(course);
                    }
                }
            }
        }


        //
        // GET: /Instructor/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        //
        // POST: /Instructor/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Instructor instructor = db.Instructors.Find(id);
            db.Instructors.Remove(instructor);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}