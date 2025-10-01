# Patient Scheduler API Tests

This directory contains unit tests for the Patient Scheduler API.

## Test Coverage

The test suite provides comprehensive coverage for the `PatientsController` API endpoints:

### Test Categories

#### 1. GetPatients Tests
- ✅ Returns empty list when no patients exist
- ✅ Returns patients list when patients exist

#### 2. GetPatient Tests  
- ✅ Returns patient data when patient exists
- ✅ Returns 404 when patient does not exist

#### 3. CreatePatient Tests
- ✅ Creates patient successfully with valid data
- ✅ Returns conflict when patient with same email exists

#### 4. UpdatePatient Tests
- ✅ Updates patient successfully with valid data
- ✅ Returns 404 when patient does not exist

#### 5. DeletePatient Tests
- ✅ Deletes patient successfully
- ✅ Returns 404 when patient does not exist

#### 6. DTO Validation Tests
- ✅ Validates CreatePatientDto with valid data
- ✅ Validates CreatePatientDto with invalid data
- ✅ Validates UpdatePatientDto with valid partial data

## Test Framework

- **Testing Framework**: xUnit
- **Mocking Framework**: Moq
- **Database**: Entity Framework Core In-Memory
- **Target Framework**: .NET 8.0

## Running Tests

### From Command Line
```bash
# Run all tests
dotnet test PatientScheduler.Tests/PatientScheduler.Tests.csproj

# Run tests with detailed output
dotnet test PatientScheduler.Tests/PatientScheduler.Tests.csproj --verbosity normal

# Run specific test
dotnet test PatientScheduler.Tests/PatientScheduler.Tests.csproj --filter "GetPatients_ReturnsOkResult_WithEmptyList_WhenNoPatientsExist"
```

### From Project Root
```bash
# Use the test runner script
./run-tests.sh
```

## Test Structure

Each test follows the **Arrange-Act-Assert** pattern:

1. **Arrange**: Set up test data and dependencies
2. **Act**: Execute the method under test
3. **Assert**: Verify the expected outcome

## Key Features

- **Isolation**: Each test uses a fresh in-memory database
- **Mocking**: Logger dependencies are mocked using Moq
- **Validation**: DTO validation is tested using DataAnnotations
- **Error Handling**: Tests cover both success and error scenarios

## Test Data

Tests use realistic patient data including:
- Personal information (name, email, phone)
- Medical history
- Address information
- Timestamps (CreatedAt, UpdatedAt)

## Coverage Statistics

- **Total Tests**: 13
- **Passing**: 13
- **Failing**: 0
- **Coverage**: All CRUD operations for Patient entity

## Dependencies

The test project references:
- `PatientScheduler` (main project)
- `Microsoft.EntityFrameworkCore.InMemory` (for testing database operations)
- `Moq` (for mocking dependencies)
- `xUnit` (testing framework)

## Best Practices

1. **Naming Convention**: Tests follow the pattern `MethodName_Scenario_ExpectedResult`
2. **Isolation**: Each test is independent and can run in any order
3. **Cleanup**: Tests properly dispose of resources
4. **Realistic Data**: Test data reflects real-world scenarios
5. **Error Cases**: Both success and failure scenarios are tested
