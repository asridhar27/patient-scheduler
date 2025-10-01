using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PatientScheduler.Controllers;
using PatientScheduler.Data;
using PatientScheduler.DTOs;
using PatientScheduler.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace PatientScheduler.Tests
{
    public class PatientsControllerTests : IDisposable
    {
        private readonly PatientSchedulerContext _context;
        private readonly PatientsController _controller;
        private readonly Mock<ILogger<PatientsController>> _mockLogger;

        public PatientsControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<PatientSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PatientSchedulerContext(options);
            _mockLogger = new Mock<ILogger<PatientsController>>();
            _controller = new PatientsController(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetPatients Tests

        [Fact]
        public async Task GetPatients_ReturnsOkResult_WithEmptyList_WhenNoPatientsExist()
        {
            // Act
            var result = await _controller.GetPatients();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patients = Assert.IsType<List<PatientResponseDto>>(okResult.Value);
            Assert.Empty(patients);
        }

        [Fact]
        public async Task GetPatients_ReturnsOkResult_WithPatientsList_WhenPatientsExist()
        {
            // Arrange
            var patients = new List<Patient>
            {
                new Patient
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "555-1234",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Address = "123 Main St",
                    MedicalHistory = "No known allergies",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    PhoneNumber = "555-5678",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Address = "456 Oak Ave",
                    MedicalHistory = "Diabetes Type 2",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.Patients.AddRange(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPatients();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDtos = Assert.IsType<List<PatientResponseDto>>(okResult.Value);
            Assert.Equal(2, patientDtos.Count);
            Assert.Equal("John", patientDtos[0].FirstName);
            Assert.Equal("Jane", patientDtos[1].FirstName);
        }

        #endregion

        #region GetPatient Tests

        [Fact]
        public async Task GetPatient_ReturnsOkResult_WhenPatientExists()
        {
            // Arrange
            var patient = new Patient
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1),
                Address = "123 Main St",
                MedicalHistory = "No known allergies",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPatient(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDto = Assert.IsType<PatientResponseDto>(okResult.Value);
            Assert.Equal(1, patientDto.Id);
            Assert.Equal("John", patientDto.FirstName);
            Assert.Equal("Doe", patientDto.LastName);
        }

        [Fact]
        public async Task GetPatient_ReturnsNotFound_WhenPatientDoesNotExist()
        {
            // Act
            var result = await _controller.GetPatient(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Patient with ID 999 not found", notFoundResult.Value);
        }

        #endregion

        #region CreatePatient Tests

        [Fact]
        public async Task CreatePatient_ReturnsCreatedAtAction_WhenValidPatientIsCreated()
        {
            // Arrange
            var createPatientDto = new CreatePatientDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1),
                Address = "123 Main St",
                MedicalHistory = "No known allergies"
            };

            // Act
            var result = await _controller.CreatePatient(createPatientDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var patientDto = Assert.IsType<PatientResponseDto>(createdAtActionResult.Value);
            Assert.Equal("John", patientDto.FirstName);
            Assert.Equal("Doe", patientDto.LastName);
            Assert.Equal("john.doe@example.com", patientDto.Email);
            Assert.True(patientDto.Id > 0);

            // Verify patient was saved to database
            var savedPatient = await _context.Patients.FindAsync(patientDto.Id);
            Assert.NotNull(savedPatient);
            Assert.Equal("John", savedPatient.FirstName);
        }

        [Fact]
        public async Task CreatePatient_ReturnsConflict_WhenPatientWithSameEmailExists()
        {
            // Arrange
            var existingPatient = new Patient
            {
                FirstName = "Existing",
                LastName = "Patient",
                Email = "existing@example.com",
                PhoneNumber = "555-9999",
                DateOfBirth = new DateTime(1980, 1, 1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(existingPatient);
            await _context.SaveChangesAsync();

            var createPatientDto = new CreatePatientDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "existing@example.com", // Same email as existing patient
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var result = await _controller.CreatePatient(createPatientDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("A patient with email existing@example.com already exists", conflictResult.Value);
        }

        #endregion

        #region UpdatePatient Tests

        [Fact]
        public async Task UpdatePatient_ReturnsOkResult_WhenValidUpdateIsMade()
        {
            // Arrange
            var patient = new Patient
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1),
                Address = "123 Main St",
                MedicalHistory = "No known allergies",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var updatePatientDto = new UpdatePatientDto
            {
                FirstName = "Johnny",
                Address = "456 New St"
            };

            // Act
            var result = await _controller.UpdatePatient(1, updatePatientDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDto = Assert.IsType<PatientResponseDto>(okResult.Value);
            Assert.Equal("Johnny", patientDto.FirstName);
            Assert.Equal("456 New St", patientDto.Address);
            Assert.Equal("Doe", patientDto.LastName); // Should remain unchanged

            // Verify database was updated
            var updatedPatient = await _context.Patients.FindAsync(1);
            Assert.Equal("Johnny", updatedPatient.FirstName);
            Assert.Equal("456 New St", updatedPatient.Address);
        }

        [Fact]
        public async Task UpdatePatient_ReturnsNotFound_WhenPatientDoesNotExist()
        {
            // Arrange
            var updatePatientDto = new UpdatePatientDto
            {
                FirstName = "Johnny"
            };

            // Act
            var result = await _controller.UpdatePatient(999, updatePatientDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Patient with ID 999 not found", notFoundResult.Value);
        }

        #endregion

        #region DeletePatient Tests

        [Fact]
        public async Task DeletePatient_ReturnsNoContent_WhenPatientExists()
        {
            // Arrange
            var patient = new Patient
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeletePatient(1);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify patient was removed from database
            var deletedPatient = await _context.Patients.FindAsync(1);
            Assert.Null(deletedPatient);
        }

        [Fact]
        public async Task DeletePatient_ReturnsNotFound_WhenPatientDoesNotExist()
        {
            // Act
            var result = await _controller.DeletePatient(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Patient with ID 999 not found", notFoundResult.Value);
        }

        #endregion

        #region DTO Validation Tests

        [Fact]
        public void CreatePatientDto_Validation_ShouldPass_WithValidData()
        {
            // Arrange
            var dto = new CreatePatientDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                DateOfBirth = new DateTime(1990, 1, 1),
                Address = "123 Main St",
                MedicalHistory = "No known allergies"
            };

            // Act & Assert
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);
            var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CreatePatientDto_Validation_ShouldFail_WithInvalidData()
        {
            // Arrange
            var dto = new CreatePatientDto
            {
                FirstName = "", // Invalid - empty
                LastName = "", // Invalid - empty
                Email = "invalid-email", // Invalid email format
                PhoneNumber = "", // Invalid - empty
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);
            var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, r => r.MemberNames.Contains("FirstName"));
            Assert.Contains(validationResults, r => r.MemberNames.Contains("LastName"));
            Assert.Contains(validationResults, r => r.MemberNames.Contains("Email"));
            Assert.Contains(validationResults, r => r.MemberNames.Contains("PhoneNumber"));
        }

        [Fact]
        public void UpdatePatientDto_Validation_ShouldPass_WithValidPartialData()
        {
            // Arrange
            var dto = new UpdatePatientDto
            {
                FirstName = "Johnny",
                Email = "johnny.doe@example.com"
            };

            // Act & Assert
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);
            var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        #endregion
    }
}
