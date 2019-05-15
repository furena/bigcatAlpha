# Azure Active Directory/ASP.Net MVC/GraphAPI B2BPortal
## Sample/Prototype project enabling self-service B2B capabilities for an Azure AD Tenant
## Quick Start

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Factive-directory-dotnet-graphapi-b2bportal-web%2Fdev%2Fazuredeploy.json" target="_blank"><img src="http://azuredeploy.net/deploybutton.png"/></a>

(NOTE: deploy link on this page points to the dev branch template. __Please ensure that you change "master" to "dev" in the "Branch" field of the ARM template form after you click to deploy.__)

__Deployment__

[Detailed step-by-step deployment instructions](./Setup.md)

__Update__

(Notes here will be reflective of any changes made in the most recent update.)

An additional permission is required for the "admin" Azure AD app (NOT the pre-auth app), to facilitate batch processing with automatic directory addition.
  * DELEGATED PERMISSIONS
    * Access the directory as the signed-in user

This permission will allow a guest user, that has been added to the "Guest Inviter" role, to invite additional
guests from their home directory. Those guests are automatically added as new guests without needing
to go through an invitation redemption process. They will have immediate, automatic access. The list of groups that can be assigned to these guests are limited to groups where the inviting guest is already a member.

After adding this permission, because user's previous consents will be invalidated, you will need to click "Grant Permissions" to update the consents for users in the tenant.

 ![alt text][Grant1]

__Details__

* Allows self-service provisioning of guest accounts in a tenant. Portal enables this via API calls to the Microsoft Graph
* Leverages Azure CosmosDB. For development, a downloadable emulator is available: https://aka.ms/documentdb-emulator
* ARM template deploys the following:
  * Azure Web App
  * Azure CosmosDB
* Requires the following (see step-by-step deployment instructions above for details):
  1. Azure AD application with the following:
    * Microsoft Graph - app permissions
      * Read and write directory data
      * Read and write all users' full profiles
    * Microsoft Graph - delegated permissions
      * Sign in and read user profile
  2. Azure AD application with the following:
    * Microsoft Graph - delegated permissions
      * Sign in and read user profile
      * Multi-Tenant enabled
  * Optional - custom DNS name and SSL cert

__Operation__

* Guests access the home page and may enter their login email to request access to the host tenant/company. Optionally, they may click to "Require Sign-In" - this will allow them to login to the guest's home tenant, authenticate, then return with the form pre-filled AND with the request authenticated and validated.
* Once the request is submitted, the request will be queued in a CosmosDB repo.
* A user in the home company with the "Guest Inviter" role granted can then access the portal, log in, and browse the pending requests, either approving, denying, or leaving in a pending state for others to review. Additionally, internal comments can be attached to the request records.
* Optionally, authorized users may login and add a "Partner Organization" profile record. This will allow potential guests with a matching domain suffix, to be optionally auto-approved for B2B guest access in the tenant.
* Whether a user is automatically approved, or manually approved, once an approval occurs, a welcome email is generated to the requester with a link that allows for redemption of the request. 

# As-Is Code

This code is made available as a sample to demonstrate usage of the Azure Active Directory B2B Invitation API. It should be customized by your dev team or a partner, and should be reviewed before being deployed in a production scenario.

# Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[Grant1]: ./DocImages/GrantPerm.png "Grant Permissions"