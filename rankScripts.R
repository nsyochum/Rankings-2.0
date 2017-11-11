cfb <- read.csv("http://students.washington.edu/nsyochum/rankings.csv", header=T)
nfl <- read.csv("http://students.washington.edu/nsyochum/nfl.csv", header=T)

cfbTeams <- createDictionary(cfb$rating, cfb$team)

createDictionary <- function(data, names) {
  l <- length(data)
  dictionary <- vector(mode="list", length = l)
  names(dictionary) = names
  for(ind in 1:l) {
    dictionary[names[ind]] <- data[ind]
  }
  
  return(dictionary)
}

homeWinRate <- function(home, away, factor) {
  rate <- (home+factor)/(home+away+factor)
  return(rate)
}
