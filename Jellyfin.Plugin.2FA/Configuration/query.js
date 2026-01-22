document.querySelector('#TwoFAEnrollButton').addEventListener('click', function () {
  var userId = document.querySelector('#UserId').value; // add a user selector input
  ApiClient.ajax({
    type: 'POST',
    url: ApiClient.getUrl('Plugins/TwoFA/users/' + userId + '/totp/enroll'),
    contentType: 'application/json'
  }).then(function (response) {
    // response should include otpauth URI
  });
});
