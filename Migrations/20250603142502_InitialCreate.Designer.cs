﻿// <auto-generated />
using System;
using EduSync.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EduSync.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250603142502_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("EduSync.Models.Assessment", b =>
                {
                    b.Property<Guid>("AssessmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CourseId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("DueDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsLive")
                        .HasColumnType("bit");

                    b.Property<int>("MaxScore")
                        .HasColumnType("int");

                    b.Property<int?>("PassingScore")
                        .HasColumnType("int");

                    b.Property<string>("SessionId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("TimeLimit")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("AssessmentId");

                    b.HasIndex("CourseId");

                    b.ToTable("Assessments");
                });

            modelBuilder.Entity("EduSync.Models.AssessmentAttempt", b =>
                {
                    b.Property<Guid>("AttemptId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AssessmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<double?>("Score")
                        .HasColumnType("float");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("TimeSpent")
                        .HasColumnType("int");

                    b.HasKey("AttemptId");

                    b.HasIndex("AssessmentId");

                    b.HasIndex("StudentId", "AssessmentId");

                    b.ToTable("AssessmentAttempts");
                });

            modelBuilder.Entity("EduSync.Models.AssessmentParticipant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AssessmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ConnectionId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("JoinedAt")
                        .HasColumnType("datetime2");

                    b.Property<int?>("Score")
                        .HasColumnType("int");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("SubmittedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("AssessmentId");

                    b.HasIndex("StudentId");

                    b.ToTable("AssessmentParticipants");
                });

            modelBuilder.Entity("EduSync.Models.Course", b =>
                {
                    b.Property<Guid>("CourseId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Category")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("InstructorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Level")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("MediaContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("MediaFileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<long?>("MediaFileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("MediaFileUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Prerequisites")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("CourseId");

                    b.HasIndex("InstructorId");

                    b.HasIndex("UserId");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("EduSync.Models.Enrollment", b =>
                {
                    b.Property<Guid>("EnrollmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CourseId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("EnrollmentDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Progress")
                        .HasColumnType("int");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("EnrollmentId");

                    b.HasIndex("CourseId");

                    b.HasIndex("StudentId");

                    b.ToTable("Enrollments");
                });

            modelBuilder.Entity("EduSync.Models.Question", b =>
                {
                    b.Property<Guid>("QuestionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AssessmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CorrectAnswer")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Options")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<string>("QuestionText")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("QuestionId");

                    b.HasIndex("AssessmentId");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("EduSync.Models.Result", b =>
                {
                    b.Property<Guid>("ResultId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Answers")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("AssessmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("AttemptDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CompletedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("bit");

                    b.Property<int>("Score")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ResultId");

                    b.HasIndex("UserId");

                    b.HasIndex("AssessmentId", "UserId");

                    b.ToTable("Results");
                });

            modelBuilder.Entity("EduSync.Models.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Bio")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfilePicture")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("UserId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("EduSync.Models.Assessment", b =>
                {
                    b.HasOne("EduSync.Models.Course", "Course")
                        .WithMany("Assessments")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Course");
                });

            modelBuilder.Entity("EduSync.Models.AssessmentAttempt", b =>
                {
                    b.HasOne("EduSync.Models.Assessment", "Assessment")
                        .WithMany("Attempts")
                        .HasForeignKey("AssessmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("EduSync.Models.User", "Student")
                        .WithMany("AssessmentAttempts")
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Assessment");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("EduSync.Models.AssessmentParticipant", b =>
                {
                    b.HasOne("EduSync.Models.Assessment", "Assessment")
                        .WithMany()
                        .HasForeignKey("AssessmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EduSync.Models.User", "Student")
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Assessment");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("EduSync.Models.Course", b =>
                {
                    b.HasOne("EduSync.Models.User", "Instructor")
                        .WithMany("InstructorCourses")
                        .HasForeignKey("InstructorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("EduSync.Models.User", null)
                        .WithMany("CreatedCourses")
                        .HasForeignKey("UserId");

                    b.Navigation("Instructor");
                });

            modelBuilder.Entity("EduSync.Models.Enrollment", b =>
                {
                    b.HasOne("EduSync.Models.Course", "Course")
                        .WithMany("Enrollments")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("EduSync.Models.User", "Student")
                        .WithMany("EnrolledCourses")
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Course");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("EduSync.Models.Question", b =>
                {
                    b.HasOne("EduSync.Models.Assessment", "Assessment")
                        .WithMany("Questions")
                        .HasForeignKey("AssessmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Assessment");
                });

            modelBuilder.Entity("EduSync.Models.Result", b =>
                {
                    b.HasOne("EduSync.Models.Assessment", "Assessment")
                        .WithMany("Results")
                        .HasForeignKey("AssessmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("EduSync.Models.User", "User")
                        .WithMany("Results")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Assessment");

                    b.Navigation("User");
                });

            modelBuilder.Entity("EduSync.Models.Assessment", b =>
                {
                    b.Navigation("Attempts");

                    b.Navigation("Questions");

                    b.Navigation("Results");
                });

            modelBuilder.Entity("EduSync.Models.Course", b =>
                {
                    b.Navigation("Assessments");

                    b.Navigation("Enrollments");
                });

            modelBuilder.Entity("EduSync.Models.User", b =>
                {
                    b.Navigation("AssessmentAttempts");

                    b.Navigation("CreatedCourses");

                    b.Navigation("EnrolledCourses");

                    b.Navigation("InstructorCourses");

                    b.Navigation("Results");
                });
#pragma warning restore 612, 618
        }
    }
}
