# RaceDay

| Project | What it is |
|---|---|
| **RaceDay.Api** | RESTful API (Part 2) + image storage in Azure Blob Storage (Part 3) |
| **RaceDay.Web** | ASP.NET Core MVC website (Part 3). It only talks to the API: no database code, no Azure code. |
| **RaceDay.Tests** | API tests, run by GitHub Actions on every push |

## 1. Run it (Visual Studio)

1. Open `RaceDay.sln`.
2. Right-click the solution -> **Configure Startup Projects** -> **Multiple startup projects** -> set **RaceDay.Api** (first) and **RaceDay.Web** to *Start*. Press **F5**.
3. The website opens at http://localhost:5200 (API + Swagger at http://localhost:5100/swagger).

**There is nothing else to set up.** On first start the API creates its own database called `RaceDayDB_Part3` on `(localdb)\assignment` (see `DefaultConnection` in `RaceDay.Api/appsettings.json`) and fills it with sample events. Your old `RaceDayDB` is not touched. No migrations, no SQL scripts.

### Demo logins (password for both: `Password123!`)

| Role | Email |
|---|---|
| Organiser | `organiser@raceday.test` |
| Participant | `runner@raceday.test` |

The sample data has four events (one already finished, with published results, so *My results* shows "7th out of 12 finishers"), and enrolments in every status so the colour coding is visible.
You can also sign up with your own account on the Sign up page.

## 2. Image storage (Azure Blob Storage)

Event banners and profile pictures are uploaded through the **API** (the website never talks to Azure). `Storage:Provider` in `RaceDay.Api/appsettings.json` decides where they go:

| Value | Behaviour |
|---|---|
| `"Auto"` (default) | Azure Blob Storage if an Azure connection string is set, otherwise the API's own `uploads` folder |
| `"Azure"` | Always Azure Blob Storage |
| `"Local"` | Always the `uploads` folder |

To use Azure: right-click **RaceDay.Api** -> *Manage User Secrets* and paste (never commit the real string):

    { "AzureBlob": { "ConnectionString": "<your storage account connection string>" } }

The container `raceday-images` is created automatically with public read access. (The free [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) emulator also works: `UseDevelopmentStorage=true`.)
The API prints which storage it is using when it starts ("Image storage: ...").

## 3. Run the website in Docker (single command)

Start the API first (step 1), then:

    docker build -t raceday-web ./RaceDay.Web
    docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5100 --add-host=host.docker.internal:host-gateway raceday-web

Open http://localhost:8080. The container behaves exactly like the local run; only the API address differs.

## 4. Tests

    dotnet test RaceDay.sln

CI (`.github/workflows/dotnet.yml`) builds, runs the tests and builds the Docker image.

## What each role can do

**Organiser** - dashboard (events, enrolments per event, upcoming dates); create / edit / delete events with a banner; add age or distance categories; see who enrolled (category + status); capture and publish finish times and positions.

**Participant** - browse upcoming events with filters; open an event to see details and categories; enrol in a category; see all enrolments and their status; see race history with finish time and position; update personal details and profile picture.

## If a page shows "Something went wrong on our side"

That is the website hiding a technical error from the user. The real reason is written to **View -> Output -> Show output from: Debug** in Visual Studio, on a line starting `RaceDay API GET ... returned 500:`. You can also try the same call in Swagger.
