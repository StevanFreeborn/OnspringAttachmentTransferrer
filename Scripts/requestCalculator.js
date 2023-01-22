var totalRecords = 50;
var numOfFieldsProcessed = 1;
var numOfFilesPerField = 10;
var pageSize = 50;
var totalPages = Math.ceil(totalRecords / pageSize);
var totalRequests = 0;

// Requests to validate match fields and flag field and values
totalRequests += 3;

var currentPage = 1;

// Loop to get each page of records
do {
  // Request to get page of records
  totalRequests += 1;
  var numOfRecordsProcessedPerPage = totalRecords <= pageSize 
  ? totalRecords 
  : pageSize;

  // Loop to process each record in the page
  for (let i = 0; i < numOfRecordsProcessedPerPage; i++) {
    // Request to get matching target record
    totalRequests += 1;
    // loop to process each field for each record in the page
    for (let j = 0; j < numOfFieldsProcessed; j++) {
      // Loop to process each file in each field for each record in the page
      for (let i = 0; i < numOfFilesPerField; i++) {
        // Requests to transfer file
        totalRequests += 3;
      }
    }
    // Request to update source record as being processed
    totalRequests += 1;
  }

  currentPage++;
} while (currentPage <= totalPages);

console.log(totalRequests);
